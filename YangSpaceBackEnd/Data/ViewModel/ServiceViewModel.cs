namespace YangSpaceBackEnd.Data.ViewModel;

public class ServiceViewModel
{
    public int ServiceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public IFormFile ImagePath { get; set; } = null!;
}