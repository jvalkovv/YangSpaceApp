using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using YangSpaceBackEnd.Data.Models;
using YangSpaceBackEnd.Data.ViewModel.AccountViewModel;

namespace YangSpaceBackEnd.Controllers;

[Route("[controller]")]
[ApiController]
public class AccountController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AccountController(UserManager<User> userManager, SignInManager<User> signInManager, IConfiguration configuration, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _roleManager = roleManager;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserRegistrationModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { message = "Invalid data." });
        }

        // Check if the username already exists
        var existingUser = await _userManager.FindByNameAsync(model.Username);
        var existingEmail = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            return Conflict(new { message = "Username is already taken." });
        }

        if (existingEmail != null)
        {
            return Conflict(new { message = "Email is already taken." });
        }

        await _roleManager.CreateAsync(new IdentityRole("ServiceProvider"));

        await _roleManager.CreateAsync(new IdentityRole("OrdinaryUser"));

        
        // Create the new user
        var user = new User
        {
            UserName = model.Username,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            Role = model.IsServiceProvider ? "ServiceProvider" : "OrdinaryUser"
        };

        // Create the user in the database
        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }


        // Assign role to the user
        var roleAssignmentResult = await _userManager.AddToRoleAsync(user, user.Role);

        if (!roleAssignmentResult.Succeeded)
        {
            return BadRequest(new { message = "Failed to assign role." });
        }

        // Generate JWT Token
        var token = await GenerateJwtToken(user);

        // Return the success response with the JWT and username
        return Ok(new
        {
            token,
            username = user.UserName,

        });
    }


    // Login the user and return JWT token
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserLoginModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { message = "Invalid login attempt." });
        }

        var user = await _userManager.FindByNameAsync(model.Username);

        if (user == null)
        {
            return Unauthorized(new { message = "Invalid username." });
        }

        var result = await _signInManager.PasswordSignInAsync(user, model.Password, false, false);

        if (result.Succeeded)
        {
            // Generate JWT Token if the login is successful
            var token = await GenerateJwtToken(user);

            return Ok(new
            {
                token,
                username = user.UserName,
                role = user.Role
            }
            );
        }

        return Unauthorized(new { message = "Invalid credentials(password)." });
    }

    // Logout the user (sign-out)
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok(new { message = "User logged out successfully." });
    }

    // Check if the username is taken
    [HttpGet("check-username/{username}")]
    public async Task<IActionResult> CheckUsername(string username)
    {
        var existingUser = await _userManager.FindByNameAsync(username);
        if (existingUser != null)
        {
            return Ok(new { isUsernameTaken = true }); // Username is already taken
        }
        return Ok(new { isUsernameTaken = false });
    }

    // Check if the username is taken
    [HttpGet("check-email/{email}")]
    public async Task<IActionResult> CheckEmail(string email)
    {
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            return Ok(new { isEmailTaken = true }); // Email is already taken
        }
        return Ok(new { isEmailTaken = false });
    }

    private async Task<string> GenerateJwtToken(User user)
    {
        // Implement JWT token generation logic here
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration["Jwt:SecretKey"]);
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Email, user.Email),
            // Add more claims as necessary (e.g., roles, permissions, etc.)
        };

        var userRoles = await _userManager.GetRolesAsync(user);
        foreach (var role in userRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.Now.AddMinutes(15), // You can adjust the expiration time
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token); // Return the JWT token as a string
    }


}