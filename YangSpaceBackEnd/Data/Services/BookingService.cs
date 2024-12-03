using AutoMapper;
using Microsoft.EntityFrameworkCore;
using YangSpaceBackEnd.Data.Models;
using YangSpaceBackEnd.Data.Services.Contracts;
using YangSpaceBackEnd.Data.ViewModel;
using static YangSpaceBackEnd.Data.Extension.Enum;

namespace YangSpaceBackEnd.Data.Services;

public class BookingService : IBookingService
{
    private readonly YangSpaceDbContext _context;
    private readonly IMapper _mapper;

    public BookingService(YangSpaceDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<BookingViewModel> CreateBookingAsync(CreateBookingRequest request)
    {
        var service = await _context.Services.FindAsync(request.ServiceId);
        if (service == null) return null;

        var userId = "user-id"; // Replace with actual user context
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

        return _mapper.Map<BookingViewModel>(booking);
    }

    public async Task<BookingViewModel> GetBookingByIdAsync(int id)
    {
        var booking = await _context.Bookings
            .Include(b => b.Service)
            .FirstOrDefaultAsync(b => b.Id == id);

        return booking == null ? null : _mapper.Map<BookingViewModel>(booking);
    }

    public async Task<IEnumerable<BookingViewModel>> GetUserBookingsAsync()
    {
        var userId = "user-id"; // Replace with actual user context
        var bookings = await _context.Bookings
            .Where(b => b.UserId == userId)
            .Include(b => b.Service)
            .ToListAsync();

        return _mapper.Map<IEnumerable<BookingViewModel>>(bookings);
    }

    public async Task<BookingViewModel> UpdateBookingStatusAsync(int id, UpdateBookingStatusRequest request)
    {
        var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == id);
        if (booking == null) return null;

        booking.Status = request.Status;
        booking.UpdatedDate = DateTime.Now;

        await _context.SaveChangesAsync();

        return _mapper.Map<BookingViewModel>(booking);
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
            Bookings = _mapper.Map<List<BookingViewModel>>(bookings)
        };
    }
}