using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using YangSpaceBackEnd.Data.Models;
using YangSpaceBackEnd.Data.ViewModel;
using YangSpaceBackEnd.Data.ViewModel.AccountViewModel;
using Enum = YangSpaceBackEnd.Data.Extension.Enum;

namespace YangSpaceBackEnd.Data.Services.UserProfileServices
{
    public class UserProfileService
    {
        private readonly UserManager<User> _userManager;
        private readonly YangSpaceDbContext _context;
        public UserProfileService(UserManager<User> userManager, YangSpaceDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<UserProfileModel> GetUserProfileAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            return new UserProfileModel
            {
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };
        }

        public async Task<bool> UpdateUserProfileAsync(string userId, UserProfileModel model)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            // Update only the fields provided in the model
            user.FirstName = !string.IsNullOrWhiteSpace(model.FirstName) ? model.FirstName : user.FirstName;
            user.LastName = !string.IsNullOrWhiteSpace(model.LastName) ? model.LastName : user.LastName;
            user.Email = !string.IsNullOrWhiteSpace(model.Email) ? model.Email : user.Email;
            user.PhoneNumber = !string.IsNullOrWhiteSpace(model.PhoneNumber) ? model.PhoneNumber : user.PhoneNumber;

            // Attempt to save changes
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                // Log errors for debugging purposes
                foreach (var error in result.Errors)
                {
                    Console.Error.WriteLine($"Error updating user {userId}: {error.Description}");
                }
            }

            return result.Succeeded;
        }

        public async Task<List<BookingViewModel>> GetBookedTasksAsync(string userId)
        {
            return await _context.Bookings
                .Where(bt => bt.UserId == userId)
                .Select(bt => new BookingViewModel
                {
                    ServiceName = "null",
                    UserId = userId,
                    UserName = "test",
                    BookingDate = default,
                    Status = Enum.BookingStatus.InProgress,
                })
                .ToListAsync();
        }
    }
}
