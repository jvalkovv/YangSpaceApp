using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YangSpaceApp.Server.Data;
using YangSpaceApp.Server.Data.Services.Contracts;
using YangSpaceApp.Server.Data.ViewModel;
using YangSpaceApp.Server.Data.ViewModel.AccountViewModel;

namespace YangSpaceApp.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserProfileController : ControllerBase
    {
        private readonly IUserProfileService _userProfileService;
        private readonly IServiceService _serviceService;
        private readonly IBookingService _bookingService;
        private readonly YangSpaceDbContext _context;
        public UserProfileController(IUserProfileService userProfileService, IServiceService serviceService, IBookingService bookingService, YangSpaceDbContext context)
        {
            _userProfileService = userProfileService;
            _serviceService = serviceService;
            _bookingService = bookingService;
            _context = context;
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

            var updatedProfile = await _userProfileService.GetUserProfileAsync(userId);

            return Ok(updatedProfile);  // Return the updated profile
        }

        // Get services that the user is offering (provider's services)
        [HttpGet("services-to-provide")]
        public async Task<IActionResult> GetServicesToProvide()
        {
            var userId = GetUserIdFromToken();

            var services = await _serviceService.GetServicesByProviderAsync(userId);

            // Include the bookings for those services
            var recentBookings = await _context.Bookings
                .Where(b => services.Select(s => s.Id).Contains(b.ServiceId))
                .Include(b => b.Service)
                .OrderByDescending(b => b.BookingDate)
                .Take(3) 
                .ToListAsync();

            return Ok(new
            {
                ProvidedServices = services,
                RecentBookings = recentBookings.Select(b => new BookingViewModel
                {
                    ServiceId = b.Service.Id,
                    ServiceName = b.Service.Title,
                    BookingDate = b.BookingDate,
                    Status = b.Status.ToString(), 
                    Price = b.Service.Price
                })
            });
        }

        // Get services that the user is booked for (booked services)
        [HttpGet("services-booked")]
        public async Task<IActionResult> GetServicesBooked()
        {
            var userId = GetUserIdFromToken();
            
            // Include the Service navigation property

            var bookings = await _bookingService
                .GetBookingsByUserId(userId);

            // Return the most recent bookings
            var recentBookings = bookings
                .OrderByDescending(b => b.BookingDate)
                .Take(3) // Limit to 3 recent bookings
                .Select(b => new BookingViewModel
                {
                    ServiceId = b.Service.Id,
                    ServiceName = b.Service.Title,
                    BookingDate = b.BookingDate,
                    Status = b.Status.ToString(),
                    Price = b.Service.Price
                })
                .ToList();

            return Ok(recentBookings);
        }


        //[HttpGet("booked-services")]
        //public async Task<IActionResult> GetBookedServices(bool isServiceProvider)
        //{

        //    var userId = GetUserIdFromToken();


        //    if (string.IsNullOrEmpty(userId))
        //    {
        //        return Unauthorized("User not authorized.");
        //    }

        //    var bookedServices = await _userProfileService.GetBookedServicesAsync(userId, isServiceProvider);

        //    return Ok(bookedServices);
        //}

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