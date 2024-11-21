using Microsoft.AspNetCore.Identity;
using YangSpaceBackEnd.Data.Models;
using YangSpaceBackEnd.Data.ViewModel.AccountViewModel;

namespace YangSpaceBackEnd.Data.Services.UserProfileServices
{
    public class UserProfileService
    {
        private readonly UserManager<User> _userManager;

        public UserProfileService(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        public async Task<User> GetUserProfileAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user;
        }

        public async Task<bool> UpdateUserProfileAsync(string userId, UserProfileModel model)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            user.FirstName = model.FirstName ?? user.FirstName;
            user.LastName = model.LastName ?? user.LastName;
            user.Email = model.Email ?? user.Email;
            user.PhoneNumber = model.PhoneNumber ?? user.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }
    }
}
