using Microsoft.AspNetCore.Mvc;

namespace YangSpaceBackEnd.Data.Services.Contracts
{
    public interface IImageService
    {
        Task<string> SaveImageAsync(IFormFile file, int serviceId);
        Task<IActionResult> GetImageAsync(string fileName);
        Task DeleteImageAsync(string fileName); // Added method for deleting images
    }
}
