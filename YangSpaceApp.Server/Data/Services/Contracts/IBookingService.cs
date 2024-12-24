using YangSpaceApp.Server.Data.Models;
using YangSpaceApp.Server.Data.ViewModel;

namespace YangSpaceApp.Server.Data.Services.Contracts
{
    public interface IBookingService
    {
        Task<bool> CreateBookingAsync(string userId, int serviceId);
        Task<BookingViewModel> GetBookingByIdAsync(int id);
        Task<IEnumerable<BookingViewModel>> GetUserBookingsAsync(string userId);
        Task<bool> UpdateBookingStatusAsync(int bookingId, UpdateBookingStatusRequest request);
        Task<PaginatedBookingsViewModel> GetBookingsAsync(string status, int page, int pageSize);
        Task<List<Booking>> GetBookingsByUserId(string? userId);
        public Task<IEnumerable<BookingViewModel>> GetBookingsForProviderAsync(string providerId);

    }
}
