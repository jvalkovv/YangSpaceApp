using Microsoft.AspNetCore.Mvc;
using YangSpaceBackEnd.Data;

namespace YangSpaceBackEnd.Controllers
{
    [ApiController]
    [Route("/[controller]")]
    public class ServicesController : ControllerBase
    {
        private readonly YangSpaceDbContext _context;

        public ServicesController(YangSpaceDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetServices()
        {
            var services = _context.Services.ToList();
            return Ok(services);
        }
    }

}
