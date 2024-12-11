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

    public async Task<bool> CreateBookingAsync(string userId, int serviceId)
    {
        var service = await _context.Services.FindAsync(serviceId);

        if (service == null) throw new Exception("Service not found.");

        // Prevent self-booking
        if (service.ProviderId == userId)
            throw new InvalidOperationException("You cannot book your own service.");

        var existingBooking = await _context.Bookings.FirstOrDefaultAsync(b =>
            b.UserId == userId && b.ServiceId == service.Id && b.Status == BookingStatus.Pending);

        if (existingBooking != null)
        {
            throw new InvalidOperationException("You have already booked this service.");
        }

        var booking = new Booking
        {
            ServiceId = serviceId,
            UserId = userId,
            Status = BookingStatus.Pending,
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        return true;
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

    public async Task<bool> UpdateBookingStatusAsync(int bookingId, UpdateBookingStatusRequest request)
    {
        var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking == null) return false;


        if (Enum.TryParse(request.Status, true, out BookingStatus status))
        { booking.Status = status; }


        if (request.ResolvedDate.HasValue)
        {
            booking.UpdatedDate = request.ResolvedDate.Value;
        }
        booking.UpdatedDate = DateTime.Now;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<PaginatedBookingsViewModel> GetBookingsAsync(string status, int page, int pageSize)
    {
        var query = _context.Bookings.AsQueryable();


        if (status != "all" && Enum.TryParse<BookingStatus>(status, out var statusEnum))
        {
            query = query.Where(b => b.Status.ToString().ToLower() == status);
        }
        else if (status != "all" && status!="pending" && status != "inprogress" && status != "completed")
        {
            // Handle invalid status case (optional, depending on your requirements)
            return new PaginatedBookingsViewModel
            {
                TotalCount = 0,
                Bookings = new List<BookingViewModel>()
            };
        }

        var totalCount = await query.CountAsync();

        var bookings = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(b => b.Service)
            .Select(b => new BookingViewModel
            {
                Id = b.Id,
                ServiceName = b.Service.Title,
                BookingDate = b.BookingDate,
                Status = b.Status.ToString(),
                Price = b.Service.Price,
                UserName = $"{b.User.FirstName} {b.User.LastName}",
                ProviderEmail = b.Service.Provider.Email,
                UpdatedDate = b.UpdatedDate
            })
            .ToListAsync();

        return new PaginatedBookingsViewModel
        {
            TotalCount = totalCount,
            Bookings = bookings
        };
    }

    public async Task<List<Booking>> GetBookingsByUserId(string? userId)
    {
        return await _context.Bookings
            .Where(b => b.UserId == userId)
            .Include(b => b.Service) // Include service data in booking
            .ToListAsync();
    }

    public async Task<IEnumerable<BookingViewModel>> GetBookingsForProviderAsync(string providerId)
    {
        var bookings = await _context.Bookings
            .Where(b => b.Service.ProviderId == providerId)
            .Include(b => b.Service)
            .Select(b => new BookingViewModel
            {
                Id = b.Id,
                ServiceName = b.Service.Title,
                BookingDate = b.BookingDate,
                Status = b.Status.ToString(),
                ClientName = b.User.FirstName,
                ClientEmail = b.User.Email

            })
            .ToListAsync();

        return bookings;
    }
}