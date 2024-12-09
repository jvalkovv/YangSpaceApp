using Microsoft.AspNetCore.Mvc;
using YangSpaceApp.Server.Data.Services.Contracts;

namespace YangSpaceApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImageController : ControllerBase
    {
        private readonly IImageService _imageService;

        public ImageController(IImageService imageService)
        {
            _imageService = imageService;
        }

        [HttpGet("{fileName}")]
        public Task<IActionResult> GetImage(string fileName)
        {
            return _imageService.GetImageAsync(fileName);
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadImage(IFormFile file, int serviceId)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file uploaded" });
            }

            // Validate file type (optional)
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(new { message = "Invalid file type" });
            }

            // Save the image and associate with serviceId
            var fileName = await _imageService.SaveImageAsync(file, serviceId);

            return Ok(new { fileName });
        }

        [HttpDelete("{fileName}")]
        public async Task<IActionResult> DeleteImage(string fileName)
        {
            await _imageService.DeleteImageAsync(fileName);
            return NoContent();  // Status code 204, no content
        }
    }
}