using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using YangSpaceApp.Server.Data.Services.Contracts;
using YangSpaceApp.Server.Data.ViewModel.AccountViewModel;

namespace YangSpaceApp.Server.Controllers
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

        [HttpPut("edit-profile")]
        public async Task<IActionResult> UpdateUserProfile([FromForm] UserProfileModel model)
        {
            var userId = GetUserIdFromToken();

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not authorized.");
            }

            // If the profile picture is provided, save it
            if (model.ProfilePicture != null)
            {
                var profilePictureUrl = await SaveProfilePictureAsync(model.ProfilePicture);
                model.ProfilePictureUrl = profilePictureUrl;
            }

            var result = await _userProfileService.UpdateUserProfileAsync(userId, model);

            if (!result)
            {
                return BadRequest("Unable to update user profile.");
            }

            // Return the updated profile
            var updatedProfile = await _userProfileService.GetUserProfileAsync(userId);

            return Ok(updatedProfile);  // Return the updated profile
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

        private async Task<string> SaveProfilePictureAsync(IFormFile profilePicture)
        {
            var userId = GetUserIdFromToken();

            // Define the file path where the image will be saved
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "profile-pictures");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(profilePicture.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await profilePicture.CopyToAsync(stream);
            }

            // Return the URL or file path where the profile picture is stored
            return $"/profile-pictures/{fileName}";
        }
    }
}