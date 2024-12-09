using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using YangSpaceApp.Server.Data.Models;
using YangSpaceApp.Server.Data.Services.Contracts;
using YangSpaceApp.Server.Data.ViewModel;

using static YangSpaceApp.Server.Data.Extension.Enum;

namespace YangSpaceApp.Server.Data.Services;

public class BookingService : IBookingService
{
    private readonly YangSpaceDbContext _context;
    private readonly IUserProfileService _userProfileService;


    public BookingService(YangSpaceDbContext context, IUserProfileService userProfileService)
    {
        _context = context;
        _userProfileService = userProfileService;
    }

    public async Task<BookingViewModel> CreateBookingAsync(CreateBookingRequest request)
    {
        var service = await _context.Services.FindAsync(request.ServiceId);

        if (service == null) return null;

        var userToken = request.UserTokenKey;

        var principal = _userProfileService.GetUserProfileAsyncByToken(userToken);

        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

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

        return null;
    }

    public async Task<BookingViewModel> GetBookingByIdAsync(int id)
    {
        var booking = await _context.Bookings
            .Include(b => b.Service)
            .FirstOrDefaultAsync(b => b.Id == id);

        return null;
    }

    public async Task<IEnumerable<BookingViewModel>> GetUserBookingsAsync(string userId)
    {
        var bookings = await _context.Bookings
            .Where(b => b.UserId == userId)
            .Include(b => b.Service)
            .ToListAsync();

        return null;
    }

    public async Task<BookingViewModel> UpdateBookingStatusAsync(int id, UpdateBookingStatusRequest request)
    {
        var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == id);
        if (booking == null) return null;

        booking.Status = request.Status;
        booking.UpdatedDate = DateTime.Now;

        await _context.SaveChangesAsync();

        return null;
    }

    public async Task<PaginatedBookingsViewModel> GetBookingsAsync(BookingStatus? status, int page, int pageSize)
    {
        var query = _context.Bookings.AsQueryable();

        if (status.HasValue)
            query = query.Where(b => b.Status == status.Value);

        var totalCount = await query.CountAsync();

        var bookings = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(b => b.Service)
            .ToListAsync();

        return new PaginatedBookingsViewModel
        {
            TotalCount = totalCount,
            Bookings = null
        };
    }

}