using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using YangSpaceBackEnd.Data.Models;

namespace YangSpaceBackEnd.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("[controller]")]
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

        // Switch between "OrdinaryUser" and "ServiceProvider" roles for a user
        [HttpPost("switch-role")]
        public async Task<IActionResult> SwitchRole(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Check if the user has the "OrdinaryUser" role
            if (await _userManager.IsInRoleAsync(user, "OrdinaryUser"))
            {
                // Remove "OrdinaryUser" role and assign "ServiceProvider" role
                var removeOrdinaryUserResult = await _userManager.RemoveFromRoleAsync(user, "OrdinaryUser");
                if (!removeOrdinaryUserResult.Succeeded)
                {
                    return BadRequest(new { errors = removeOrdinaryUserResult.Errors.Select(e => e.Description) });
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
                // Remove "ServiceProvider" role and assign "OrdinaryUser" role
                var removeServiceProviderResult = await _userManager.RemoveFromRoleAsync(user, "ServiceProvider");
                if (!removeServiceProviderResult.Succeeded)
                {
                    return BadRequest(new { errors = removeServiceProviderResult.Errors.Select(e => e.Description) });
                }

                var addOrdinaryUserResult = await _userManager.AddToRoleAsync(user, "OrdinaryUser");
                if (addOrdinaryUserResult.Succeeded)
                {
                    return Ok(new { message = "User role switched to 'OrdinaryUser' successfully." });
                }

                return BadRequest(new { errors = addOrdinaryUserResult.Errors.Select(e => e.Description) });
            }

            return BadRequest(new { message = "User does not have either 'OrdinaryUser' or 'ServiceProvider' role." });
        }
    }
}
