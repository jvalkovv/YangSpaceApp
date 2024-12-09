using System.ComponentModel.DataAnnotations;

namespace YangSpaceApp.Server.Data.ViewModel;

public class ServiceViewModel
{
    public int ServiceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public IFormFile ImageFile { get; set; } = null!;

    // For returning the image path
    public string? ImageUrl { get; set; }
}