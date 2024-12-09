using YangSpaceApp.Server.Data.Models;
using YangSpaceApp.Server.Data.ViewModel.AccountViewModel;

namespace YangSpaceApp.Server.Data.Services.Contracts
{
    public interface IUserService
    {
        Task<User> RegisterUserAsync(UserRegistrationModel model);
        Task<string> GenerateJwtToken(User user);
        Task<User> LoginUserAsync(UserLoginModel model);
        Task<bool> IsUsernameTakenAsync(string username);
        Task<bool> IsEmailTakenAsync(string email);
        Task LogoutUserAsync();
    }
}
