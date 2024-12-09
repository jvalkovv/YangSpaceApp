using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using YangSpaceApp.Server.Data.Models;
using YangSpaceApp.Server.Data.ViewModel.AccountViewModel;

namespace YangSpaceApp.Server.Data.Services.Contracts
{
    public interface IUserProfileService
    {
        Task<User?> GetUserProfileAsync(string userId);
        ClaimsPrincipal GetUserProfileAsyncByToken(string token);

        Task<bool> UpdateUserProfileAsync(string userId, UserProfileModel model);
        Task<List<Booking>> GetBookedServicesAsync(string userId, bool isServiceProvider);
    }
}
