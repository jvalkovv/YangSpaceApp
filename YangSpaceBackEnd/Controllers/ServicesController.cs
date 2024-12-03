using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YangSpaceBackEnd.Data;
using YangSpaceBackEnd.Data.Extension;
using YangSpaceBackEnd.Data.Models;
using YangSpaceBackEnd.Data.ViewModel;

namespace YangSpaceBackEnd.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ServicesController : ControllerBase
    {
        private readonly YangSpaceDbContext _context;
        private readonly IConfiguration _configuration;
        public ServicesController(YangSpaceDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }
        [HttpGet]
        [Route("categories")]
        public IActionResult GetCategories()
        {
            var categories = _context.Categories.ToList();
            return Ok(categories);
        }
        // GET: Services/Provider
        [HttpGet("provider")]
        public async Task<IActionResult> GetProviderServices()
        {
            var token = Request.Headers["Authorization"].ToString();

            var principal = JwtHelper.GetPrincipalFromToken(token, _configuration["Jwt:SecretKey"]!);

            if (principal == null)
            {
                return Unauthorized(new { message = "Invalid token." });
            }

            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var services = await _context.Services
                .Where(s => s.ProviderId == userId)
                .ToListAsync();

            return Ok(services);
        }

        // GET: /Services
        [HttpGet("all-services")]
        public async Task<IActionResult> GetServices(
            [FromQuery] string? category,
            [FromQuery] string? search,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] string? sortBy,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12)
        {
            var query = _context.Services.Include(c => c.Category).Include(p => p.Provider).AsQueryable();

            if (!string.IsNullOrEmpty(category))
                query = query.Where(s => s.Category.Name == category);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(s => s.Title.Contains(search) || s.Description.Contains(search));

            if (minPrice.HasValue)
                query = query.Where(s => s.Price >= minPrice);

            if (maxPrice.HasValue)
                query = query.Where(s => s.Price <= maxPrice);

            query = sortBy?.ToLower() switch
            {
                "price" => query.OrderBy(s => s.Price),
                "name" => query.OrderBy(s => s.Title),
                "recent" or null => query.OrderByDescending(s => s.CreatedAt),
                _ => query
            };

            var totalCount = await query.CountAsync();
            var services = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new
                {
                    id = s.Id,
                    title = s.Title,
                    desciption = s.Description,
                    price = s.Price,
                    CategoryName = s.Category.Name,
                    ProviderName = $"{s.Provider.FirstName} {s.Provider.LastName}"
                })
                .ToListAsync();

            if (totalCount == 0)
            {
                return BadRequest("No services");
            }

            return Ok(new
            {
                TotalCount = totalCount,
                Services = services,
            });
        }



        // POST: /Services
        [HttpPost("create-service")]
        [Authorize(Roles = "ServiceProvider")]
        public async Task<IActionResult> CreateService([FromBody] ServiceViewModel serviceModel)
        {
            try
            {
                var principal = GetAuthenticatedUser();
                EnsureUserIsServiceProvider(principal);

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var service = new Service
                {
                    Title = serviceModel.Title,
                    Description = serviceModel.Description,
                    Price = serviceModel.Price,
                    ProviderId = userId,
                    CategoryId = serviceModel.CategoryId
                };

                _context.Services.Add(service);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetServices), new { id = service.Id }, service);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
        }

        // PUT: api/Services/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateService(int id, Service service)
        {
            if (id != service.Id)
                return BadRequest("Service ID mismatch");

            _context.Entry(service).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Services.Any(s => s.Id == id))
                    return NotFound("Service not found");

                throw;
            }

            return NoContent();
        }

        // DELETE: api/Services/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteService(int id)
        {
            var service = await _context.Services.FindAsync(id);

            if (service == null)
                return NotFound("Service not found");

            _context.Services.Remove(service);
            await _context.SaveChangesAsync();

            return NoContent();
        }


        private ClaimsPrincipal GetAuthenticatedUser()
        {
            // Check if the user is authenticated
            var token = Request.Headers["Authorization"].ToString();

            var principal = JwtHelper.GetPrincipalFromToken(token, _configuration["Jwt:SecretKey"]!);

            if (principal == null)
                throw new UnauthorizedAccessException("Invalid token.");

            if (string.IsNullOrEmpty(token))
                throw new UnauthorizedAccessException("Missing token.");

            return principal;
        }

        private static void EnsureUserIsServiceProvider(ClaimsPrincipal principal)
        {
            // Check if the user is a service provider (you could use a claim or role)
            var isServiceProvider = principal.IsInRole("ServiceProvider");
            if (!isServiceProvider)
                throw new UnauthorizedAccessException("You must be a service provider to perform this action.");
        }
    }
}
