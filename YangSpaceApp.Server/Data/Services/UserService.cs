using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using YangSpaceApp.Server.Data.Models;
using YangSpaceApp.Server.Data.Services.Contracts;
using YangSpaceApp.Server.Data.ViewModel.AccountViewModel;

namespace YangSpaceApp.Server.Data.Services;

public class UserService : IUserService
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UserService(UserManager<User> userManager, SignInManager<User> signInManager, IConfiguration configuration, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _roleManager = roleManager;
    }

    public async Task<User> RegisterUserAsync(UserRegistrationModel model)
    {
        var existingUser = await _userManager.FindByNameAsync(model.Username);
        if (existingUser != null) throw new Exception("Username is already taken.");

        var existingEmail = await _userManager.FindByEmailAsync(model.Email);
        if (existingEmail != null) throw new Exception("Email is already taken.");

        var user = new User
        {
            UserName = model.Username,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            Role = model.IsServiceProvider ? "ServiceProvider" : "Client"
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded) throw new Exception("User creation failed.");

        await _roleManager.CreateAsync(new IdentityRole("Client"));
        await _roleManager.CreateAsync(new IdentityRole("ServiceProvider"));

        var roleAssignmentResult = await _userManager.AddToRoleAsync(user, user.Role);
        if (!roleAssignmentResult.Succeeded) throw new Exception("Failed to assign role.");

        return user;
    }

    public async Task<string> GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration["Jwt:SecretKey"]);
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var userRoles = await _userManager.GetRolesAsync(user);
        foreach (var role in userRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.Now.AddMinutes(15),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public async Task<User> LoginUserAsync(UserLoginModel model)
    {
        var user = await _userManager.FindByNameAsync(model.Username);
        if (user == null) throw new UnauthorizedAccessException("Invalid username.");

        var result = await _signInManager.PasswordSignInAsync(user, model.Password, false, false);
        if (!result.Succeeded) throw new UnauthorizedAccessException("Invalid password.");

        return user;
    }
    public async Task LogoutUserAsync()
    {
        await _signInManager.SignOutAsync();
    }
    public async Task<bool> IsUsernameTakenAsync(string username) => await _userManager.FindByNameAsync(username) != null;


    public async Task<bool> IsEmailTakenAsync(string email) => await _userManager.FindByEmailAsync(email) != null;
}