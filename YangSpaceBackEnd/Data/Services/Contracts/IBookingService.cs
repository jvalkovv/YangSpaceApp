using static YangSpaceBackEnd.Data.Extension.Enum;
using YangSpaceBackEnd.Data.ViewModel;

namespace YangSpaceBackEnd.Data.Services.Contracts
{
    public interface IBookingService
    {
        Task<BookingViewModel> CreateBookingAsync(CreateBookingRequest request);
        Task<BookingViewModel> GetBookingByIdAsync(int id);
        Task<IEnumerable<BookingViewModel>> GetUserBookingsAsync();
        Task<BookingViewModel> UpdateBookingStatusAsync(int id, UpdateBookingStatusRequest request);
        Task<PaginatedBookingsViewModel> GetBookingsAsync(BookingStatus? status, int page, int pageSize);


    }
}
