using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using YangSpaceApp.Server.Data.Models;

namespace YangSpaceApp.Server.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly UserManager<User> _userManager;

        public AdminController(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        // Assign a specific role to a user
        [HttpPost("assign-role")]
        public async Task<IActionResult> AssignRole(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            if (!await _userManager.IsInRoleAsync(user, role))
            {
                var result = await _userManager.AddToRoleAsync(user, role);
                if (result.Succeeded)
                {
                    return Ok(new { message = $"Role '{role}' assigned to user '{user.UserName}' successfully." });
                }

                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
            }

            return Conflict(new { message = "User already has this role." });
        }

        // Remove a specific role from a user
        [HttpPost("remove-role")]
        public async Task<IActionResult> RemoveRole(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            if (await _userManager.IsInRoleAsync(user, role))
            {
                var result = await _userManager.RemoveFromRoleAsync(user, role);
                if (result.Succeeded)
                {
                    return Ok(new { message = $"Role '{role}' removed from user '{user.UserName}' successfully." });
                }

                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
            }

            return Conflict(new { message = "User does not have this role." });
        }

        // Switch between "Client" and "ServiceProvider" roles for a user
        [HttpPost("switch-role")]
        public async Task<IActionResult> SwitchRole(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Check if the user has the "Client" role
            if (await _userManager.IsInRoleAsync(user, "Client"))
            {
                // Remove "Client" role and assign "ServiceProvider" role
                var removeClientResult = await _userManager.RemoveFromRoleAsync(user, "Client");
                if (!removeClientResult.Succeeded)
                {
                    return BadRequest(new { errors = removeClientResult.Errors.Select(e => e.Description) });
                }

                var addServiceProviderResult = await _userManager.AddToRoleAsync(user, "ServiceProvider");
                if (addServiceProviderResult.Succeeded)
                {
                    return Ok(new { message = "User role switched to 'ServiceProvider' successfully." });
                }

                return BadRequest(new { errors = addServiceProviderResult.Errors.Select(e => e.Description) });
            }
            // Check if the user has the "ServiceProvider" role
            else if (await _userManager.IsInRoleAsync(user, "ServiceProvider"))
            {
                // Remove "ServiceProvider" role and assign "Client" role
                var removeServiceProviderResult = await _userManager.RemoveFromRoleAsync(user, "ServiceProvider");
                if (!removeServiceProviderResult.Succeeded)
                {
                    return BadRequest(new { errors = removeServiceProviderResult.Errors.Select(e => e.Description) });
                }

                var addClientResult = await _userManager.AddToRoleAsync(user, "Client");
                if (addClientResult.Succeeded)
                {
                    return Ok(new { message = "User role switched to 'Client' successfully." });
                }

                return BadRequest(new { errors = addClientResult.Errors.Select(e => e.Description) });
            }

            return BadRequest(new { message = "User does not have either 'Client' or 'ServiceProvider' role." });
        }
    }
}
