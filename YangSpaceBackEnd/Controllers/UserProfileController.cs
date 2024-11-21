using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YangSpaceBackEnd.Data.Services.UserProfileServices;
using YangSpaceBackEnd.Data.ViewModel.AccountViewModel;

namespace YangSpaceBackEnd.Controllers
{
    [Route("/[controller]")]
    [ApiController]
    public class UserProfileController : Controller
    {
        private readonly UserProfileService _userProfileService;

        public UserProfileController(UserProfileService userProfileService)
        {
            _userProfileService = userProfileService;
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized(new { message = "Invalid token." });
            }

            var user = await _userProfileService.GetUserProfileAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            return Ok(new
            {
                user.Id,
                user.UserName,
                user.FirstName,
                user.LastName,
                user.Email,
                user.PhoneNumber
            });
        }

        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UserProfileModel model)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized(new { message = "Invalid token." });
            }

            var result = await _userProfileService.UpdateUserProfileAsync(userId, model);
            if (!result)
            {
                return BadRequest(new { message = "Failed to update user." });
            }

            return Ok(new { message = "Profile updated successfully." });
        }
    }
}
