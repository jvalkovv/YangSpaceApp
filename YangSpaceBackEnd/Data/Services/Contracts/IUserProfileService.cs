using Microsoft.AspNetCore.Identity;
using YangSpaceBackEnd.Data.Models;
using YangSpaceBackEnd.Data.ViewModel.AccountViewModel;

namespace YangSpaceBackEnd.Data.Services.Contracts
{
    public interface IUserProfileService
    {
        Task<User?> GetUserProfileAsync(string userId);
        Task<bool> UpdateUserProfileAsync(string userId, UserProfileModel model);
        Task<List<Models.Booking>> GetBookedServicesAsync(string userId, bool isServiceProvider);
    }
}
