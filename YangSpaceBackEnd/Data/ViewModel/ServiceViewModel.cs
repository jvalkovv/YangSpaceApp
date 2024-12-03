namespace YangSpaceBackEnd.Data.ViewModel
{
    public class ServiceViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int CategoryId { get; set; }
        public string? ProviderId { get; set; }
    }
}
