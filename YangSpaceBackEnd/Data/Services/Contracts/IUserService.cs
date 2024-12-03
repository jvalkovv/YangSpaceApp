using YangSpaceBackEnd.Data.Models;
using YangSpaceBackEnd.Data.ViewModel.AccountViewModel;

namespace YangSpaceBackEnd.Data.Services.Contracts
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
