namespace YangSpaceBackEnd.Data.ViewModel
{
    public class ServiceViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string CategoryName { get; set; } // Include category name instead of CategoryId
        public string ProviderName { get; set; } // Include provider's name instead of ProviderId
    }
}
