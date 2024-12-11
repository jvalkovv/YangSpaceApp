using static YangSpaceApp.Server.Data.Extension.Enum;
using YangSpaceApp.Server.Data.Services;
using YangSpaceApp.Server.Data.ViewModel;
using YangSpaceApp.Server.Data.Models;

namespace YangSpaceApp.Server.Data.Services.Contracts
{
    public interface IBookingService
    {
        Task<bool> CreateBookingAsync(string userId, int serviceId);
        Task<BookingViewModel> GetBookingByIdAsync(int id);
        Task<IEnumerable<BookingViewModel>> GetUserBookingsAsync(string userId);
        Task<BookingViewModel> UpdateBookingStatusAsync(int id, UpdateBookingStatusRequest request);
        Task<PaginatedBookingsViewModel> GetBookingsAsync(BookingStatus? status, int page, int pageSize);
        Task<List<Booking>> GetBookingsByUserId(string? userId);
    }
}
