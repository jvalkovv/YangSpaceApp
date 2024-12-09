using Microsoft.AspNetCore.Mvc;

namespace YangSpaceApp.Server.Data.Services.Contracts
{
    public interface IImageService
    {
        Task<string> SaveImageAsync(IFormFile file, int serviceId);
        Task<IActionResult> GetImageAsync(string fileName);
        Task DeleteImageAsync(string fileName); // Added method for deleting images
    }
}
