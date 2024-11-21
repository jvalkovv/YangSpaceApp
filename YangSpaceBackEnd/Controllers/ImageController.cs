using Microsoft.AspNetCore.Mvc;

namespace YangSpaceBackEnd.Controllers
{
    [ApiController]
    [Route("/[controller]")]
    public class ImageController : Controller
    {
        private readonly IWebHostEnvironment _environment;

        public ImageController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [HttpGet("{fileName}")]
        public IActionResult GetImage(string fileName)
        {

            var filePath = Path.Combine(_environment.WebRootPath, "images", fileName);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new { message = "Image not found" });
            }

            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            var contentType = "image/jpg"; // Adjust this based on file type if needed

            return File(fileBytes, contentType);
        }
    }
}
