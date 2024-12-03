using Microsoft.AspNetCore.Mvc;
using YangSpaceBackEnd.Data.Services.Contracts;
using YangSpaceBackEnd.Data.ViewModel.AccountViewModel;

namespace YangSpaceBackEnd.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class UserProfileController : ControllerBase
    {
        private readonly IUserProfileService _userProfileService;

        public UserProfileController(IUserProfileService userProfileService)
        {
            _userProfileService = userProfileService;
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserProfile(string userId)
        {
            var userProfile = await _userProfileService.GetUserProfileAsync(userId);
            if (userProfile == null)
            {
                return NotFound("User not found.");
            }
            return Ok(userProfile);
        }

        [HttpPut("{userId}")]
        public async Task<IActionResult> UpdateUserProfile(string userId, UserProfileModel model)
        {
            var result = await _userProfileService.UpdateUserProfileAsync(userId, model);
            if (!result)
            {
                return BadRequest("Unable to update user profile.");
            }
            return NoContent();  // 204 No Content if successful
        }

        [HttpGet("{userId}/booked-services")]
        public async Task<IActionResult> GetBookedServices(string userId, bool isServiceProvider)
        {
            var bookedServices = await _userProfileService.GetBookedServicesAsync(userId, isServiceProvider);
            return Ok(bookedServices);
        }
    }
}