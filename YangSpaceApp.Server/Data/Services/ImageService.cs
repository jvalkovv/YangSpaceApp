using Microsoft.AspNetCore.Mvc;
using System.IO;
using YangSpaceApp.Server.Data.Services.Contracts;

namespace YangSpaceApp.Server.Data.Services
{
    public class ImageService : IImageService
    {
        private readonly IWebHostEnvironment _environment;

        public ImageService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string> SaveImageAsync(IFormFile file, int serviceId)
        {
            if (file == null || file.Length == 0) return string.Empty;

            // Create a folder for the service if it doesn't exist
            var serviceFolderPath = Path.Combine(_environment.WebRootPath, "images", serviceId.ToString());
            if (!Directory.Exists(serviceFolderPath))
            {
                Directory.CreateDirectory(serviceFolderPath);
            }

            var filePath = Path.Combine(serviceFolderPath, file.FileName);

            // Save the file to the directory
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/images/{serviceId}/{file.FileName}";  // Return the relative path
        }

        public async Task<IActionResult> GetImageAsync(string fileName)
        {
            var filePath = Path.Combine(_environment.WebRootPath, "images", fileName);
            if (!File.Exists(filePath))
            {
                return new NotFoundObjectResult(new { message = "Image not found" });
            }

            var fileBytes = File.ReadAllBytes(filePath);
            var contentType = "image/jpg";  // You can adjust this depending on your file types.
            return new FileContentResult(fileBytes, contentType);
        }

        public async Task DeleteImageAsync(string fileName)
        {
            var filePath = Path.Combine(_environment.WebRootPath, "images", fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}
