namespace YangSpaceApp.Server.Data.ViewModel
{
    public class PaginatedBookingsViewModel
    {
        public int TotalCount { get; set; }
        public List<BookingViewModel> Bookings { get; set; } = new List<BookingViewModel>();

    }
}
