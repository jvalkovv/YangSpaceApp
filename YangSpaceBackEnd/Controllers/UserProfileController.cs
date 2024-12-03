using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using YangSpaceBackEnd.Data.Extension;
using YangSpaceBackEnd.Data.Services.UserProfileServices;
using YangSpaceBackEnd.Data.ViewModel.AccountViewModel;

namespace YangSpaceBackEnd.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class UserProfileController : ControllerBase
    {
        private readonly UserProfileService _userProfileService;
        
        private readonly IConfiguration _configuration;
        public UserProfileController(UserProfileService userProfileService, IConfiguration configuration)
        {
            _userProfileService = userProfileService;
            _configuration = configuration;
        }

        [HttpGet("user-profile")]
        public async Task<IActionResult> GetProfile()
        {
            var token = Request.Headers["Authorization"].ToString();

            var principal = JwtHelper.GetPrincipalFromToken(token, _configuration["Jwt:SecretKey"]!);

            if (principal == null)
            {
                return Unauthorized(new { message = "Invalid token." });
            }

            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;


            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Invalid or missing token." });
            }

            var user = await _userProfileService.GetUserProfileAsync(userId);

            if (user == null)
            {
                return NotFound(new { message = "User profile not found." });
            }
            return Ok(new
            {
                username = user.UserName,
                firstName = user.FirstName,
                lastName = user.LastName,
                email = user.Email,
                phoneNumber = user.PhoneNumber,
                role = user.Role
            });
        }


        [HttpPut("user-profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UserProfileModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var token = Request.Headers["Authorization"].ToString();

            var principal = JwtHelper.GetPrincipalFromToken(token, _configuration["Jwt:SecretKey"]!);

            if (principal == null)
            {
                return Unauthorized(new { message = "Invalid token." });
            }

            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Invalid or missing token." });
            }

            var result = await _userProfileService.UpdateUserProfileAsync(userId, model);
            if (!result)
            {
                return BadRequest(new { message = "Failed to update user profile." });
            }

            return Ok(new { message = "Profile updated successfully." });
        }

        // Will be implement  also 
        //[HttpGet("booked-tasks")]
        //public async Task<IActionResult> GetBookedTasks()
        //{
        //    var userId = User.FindFirst("id")?.Value;
        //    if (userId == null) return Unauthorized();

        //    var tasks = await _userProfileService.GetBookedTasksAsync(userId);
        //    return Ok(tasks);
        //}
    }
}
