using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YangSpaceBackEnd.Data;
using YangSpaceBackEnd.Data.Models;

namespace YangSpaceBackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServicesController : ControllerBase
    {
        private readonly YangSpaceDbContext _context;

        public ServicesController(YangSpaceDbContext context)
        {
            _context = context;
        }

        // GET: api/Services/Provider
        [HttpGet("Provider")]
        public async Task<IActionResult> GetProviderServices()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var services = await _context.Services
                .Where(s => s.ProviderId == userId)
                .ToListAsync();

            return Ok(services);
        }

        // GET: api/Services
        [HttpGet]
        public async Task<IActionResult> GetServices(
            [FromQuery] string? category,
            [FromQuery] string? search,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] string? sortBy,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = _context.Services.AsQueryable();

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
                _ => query
            };

            var totalCount = await query.CountAsync();
            var services = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                TotalCount = totalCount,
                Services = services
            });
        }

        // POST: api/Services
        [HttpPost]
        public async Task<IActionResult> CreateService(Service service)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _context.Services.Add(service);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetServices), new { id = service.Id }, service);
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
    }
}
