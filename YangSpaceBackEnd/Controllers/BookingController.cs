using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YangSpaceBackEnd.Data;
using YangSpaceBackEnd.Data.Models;
using YangSpaceBackEnd.Data.Services.Booking;
using YangSpaceBackEnd.Data.ViewModel;
using static YangSpaceBackEnd.Data.Extension.Enum;

namespace YangSpaceBackEnd.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class BookingController : ControllerBase
    {
        private readonly YangSpaceDbContext _context;

        public BookingController(YangSpaceDbContext context)
        {
            _context = context;
        }

        // 1. Create a Booking (POST)
        [HttpPost]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var service = await _context.Services.FindAsync(request.ServiceId);
            if (service == null)
                return NotFound(new { Error = "Service not found" });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var booking = new Booking
            {
                ServiceId = request.ServiceId,
                UserId = userId,
                BookingDate = DateTime.Now,
                Status = BookingStatus.Pending,
                Notes = request.Notes
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            var dto = await MapToDtoAsync(booking.Id);
            return CreatedAtAction(nameof(GetBookingById), new { id = booking.Id }, dto);
        }

        // 2. Get Booking by ID (GET)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBookingById(int id)
        {
            var dto = await MapToDtoAsync(id);
            if (dto == null)
                return NotFound();

            return Ok(dto);
        }

        // 3. Get All Bookings for a User (GET)
        [HttpGet("user")]
        public async Task<IActionResult> GetUserBookings()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var bookings = await _context.Bookings
                .Where(b => b.UserId == userId)
                .Include(b => b.Service)
                .Select(b => new BookingViewModel
                {
                    Id = b.Id,
                    ServiceId = b.ServiceId,
                    ServiceName = b.Service.Title,
                    BookingDate = b.BookingDate,
                    Status = b.Status,
                    Notes = b.Notes,
                    UpdatedDate = b.UpdatedDate
                })
                .ToListAsync();

            return Ok(bookings);
        }

        // 4. Update Booking Status (PUT)
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateBookingStatus(int id, [FromBody] UpdateBookingStatusRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var booking = await _context.Bookings
                .Include(b => b.Service)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
                return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId != booking.Service.ProviderId)
                return Forbid();

            booking.Status = request.Status;
            booking.UpdatedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(await MapToDtoAsync(id));
        }

        // 5. Filtered List of Bookings with Pagination (GET)
        [HttpGet]
        public async Task<IActionResult> GetBookings([FromQuery] BookingStatus? status = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var query = _context.Bookings.Include(b => b.Service).AsQueryable();

            if (status.HasValue)
                query = query.Where(b => b.Status == status.Value);

            var totalCount = await query.CountAsync();

            var bookings = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BookingViewModel
                {
                    Id = b.Id,
                    ServiceId = b.ServiceId,
                    ServiceName = b.Service.Title,
                    BookingDate = b.BookingDate,
                    Status = b.Status,
                    Notes = b.Notes,
                    UpdatedDate = b.UpdatedDate
                })
                .ToListAsync();

            return Ok(new { TotalCount = totalCount, Bookings = bookings });
        }

        // Helper: Map Booking to DTO
        private async Task<BookingViewModel?> MapToDtoAsync(int id)
        {
            return await _context.Bookings
                .Where(b => b.Id == id)
                .Include(b => b.Service)
                .Include(b => b.User)
                .Select(b => new BookingViewModel
                {
                    Id = b.Id,
                    ServiceId = b.ServiceId,
                    ServiceName = b.Service.Title,
                    UserId = b.UserId,
                    UserName = b.User.UserName,
                    BookingDate = b.BookingDate,
                    Status = b.Status,
                    Notes = b.Notes,
                    UpdatedDate = b.UpdatedDate
                })
                .FirstOrDefaultAsync();
        }
    }
}
