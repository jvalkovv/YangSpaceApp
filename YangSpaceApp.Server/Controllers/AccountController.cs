using Microsoft.AspNetCore.Mvc;
using YangSpaceApp.Server.Data.Services.Contracts;
using YangSpaceApp.Server.Data.ViewModel.AccountViewModel;

namespace YangSpaceApp.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AccountController(IUserService userService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserRegistrationModel model)
    {
        try
        {
            var user = await userService.RegisterUserAsync(model);
            var token = await userService.GenerateJwtToken(user);
            return Ok(new { token, username = user.UserName, role = user.Role });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserLoginModel model)
    {
        try
        {
            var user = await userService.LoginUserAsync(model);
            var token = await userService.GenerateJwtToken(user);

            return Ok(new { token, username = user.UserName, role = user.Role });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await userService.LogoutUserAsync();
        return Ok(new { message = "User logged out successfully." });
    }

    [HttpGet("check-username/{username}")]
    public async Task<IActionResult> CheckUsername(string username)
    {
        bool isUsernameTaken = await userService.IsUsernameTakenAsync(username);
        return Ok(new { isUsernameTaken });
    }

    [HttpGet("check-email/{email}")]
    public async Task<IActionResult> CheckEmail(string email)
    {
        bool isEmailTaken = await userService.IsEmailTakenAsync(email);
        return Ok(new { isEmailTaken });
    }
}