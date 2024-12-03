using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using YangSpaceBackEnd.Data.Extension;
using YangSpaceBackEnd.Data.Services.Contracts;
using YangSpaceBackEnd.Data.ViewModel.AccountViewModel;

namespace YangSpaceBackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserProfileController : ControllerBase
    {
        private readonly IUserProfileService _userProfileService;

        public UserProfileController(IUserProfileService userProfileService)
        {
            _userProfileService = userProfileService;
        }

        [HttpGet("user-profile")]
        public async Task<IActionResult> GetUserProfile()
        {
            var userId = GetUserIdFromToken();

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not authorized.");
            }
            var userProfile = await _userProfileService.GetUserProfileAsync(userId);
            if (userProfile == null)
            {
                return NotFound("User not found.");
            }
            return Ok(userProfile);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateUserProfile(UserProfileModel model)
        {
            var userId = GetUserIdFromToken();

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not authorized.");
            }

            var result = await _userProfileService.UpdateUserProfileAsync(userId, model);
            if (!result)
            {
                return BadRequest("Unable to update user profile.");
            }

            return NoContent();
        }

        [HttpGet("booked-services")]
        public async Task<IActionResult> GetBookedServices(bool isServiceProvider)
        {

            var userId = GetUserIdFromToken();


            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not authorized.");
            }

            var bookedServices = await _userProfileService.GetBookedServicesAsync(userId, isServiceProvider);

            return Ok(bookedServices);
        }

        private string? GetUserIdFromToken()
        {
            var userToken = Request.Headers["Authorization"].ToString();
            var principal = _userProfileService.GetUserProfileAsyncByToken(userToken);

            return principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}