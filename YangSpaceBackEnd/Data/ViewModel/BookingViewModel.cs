namespace YangSpaceBackEnd.Data.ViewModel
{
    public class BookingViewModel
    {
        public int Id { get; set; }
        public DateTime BookingDate { get; set; }
        public string ServiceName { get; set; }
        public string UserName { get; set; }
        public string Status { get; set; }
    }
}
