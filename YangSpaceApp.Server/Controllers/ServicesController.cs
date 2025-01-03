﻿using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using YangSpaceApp.Server.Data.Extension;
using YangSpaceApp.Server.Data.Models;
using YangSpaceApp.Server.Data.Services.Contracts;
using YangSpaceApp.Server.Data.ViewModel;

namespace YangSpaceApp.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServicesController : ControllerBase
{

    private readonly IServiceService _serviceService;
    private readonly IConfiguration _configuration;
    private readonly UserManager<User> _userManager;
    private readonly IBookingService _bookingService;

    public ServicesController(IServiceService serviceService, IConfiguration configuration, UserManager<User> userManager, IBookingService bookingService)
    {
        _serviceService = serviceService;
        _configuration = configuration;
        _userManager = userManager;
        _bookingService = bookingService;
    }

    [HttpGet("categories")]
    public async Task<List<Category>> GetCategories()
    {
        return await _serviceService.GetCategories();
    }

    [HttpGet("provider")]
    public async Task<IActionResult> GetProviderServices()
    {
        string? userId = GetAuthenticatedUserId();
        var services = await _serviceService.GetServicesByProviderAsync(userId);
        return Ok(services);
    }

    [HttpGet("all-services")]
    public async Task<IActionResult> GetServices(
        [FromQuery] string? category,
        [FromQuery] string? search,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] string? sortBy,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12)
    {
        var services = await _serviceService.GetPagedServicesAsync(page, pageSize, category, search, minPrice, maxPrice, sortBy);
        return Ok(services);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetServiceById(int id)
    {
        var service = await _serviceService.GetServiceWithImageAsync(id);

        if (service == null)
        {
            return NotFound();
        }

        return Ok(service);
    }
    [HttpGet("get-service/{id}")]
    public async Task<IActionResult> GetService(int id)
    {
        var service = await _serviceService.GetServiceWithImageAsync(id);

        if (service == null)
        {
            return NotFound();
        }

        return Ok(service);
    }

    [HttpPost("book/{serviceId}")]
    public async Task<IActionResult> BookService(int serviceId)
    {
        var userId = GetAuthenticatedUserId();

        try
        {
            var success = await _bookingService.CreateBookingAsync(userId, serviceId);

            if (success)
            {
                return Ok(new { success = true, message = "Service booked successfully." });
            }
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception)
        {
            return NotFound(new { error = "Service not found." });
        }

        return BadRequest("Booking failed.");
    }


    [HttpGet("check-access/{serviceId}")]
    public async Task<IActionResult> CheckUserAccessToService(int serviceId)
    {
        string? userId = GetAuthenticatedUserId();
        var user = await _userManager.FindByIdAsync(userId);

        var hasAccess = await _serviceService.CheckUserAccessToServiceAsync(user, serviceId);
        if (!hasAccess) return Unauthorized(hasAccess);

        return Ok(hasAccess);
    }

    [HttpPost("create-service")]
    //[Authorize(Roles = "ServiceProvider")]
    public async Task<IActionResult> CreateService([FromForm] ServiceViewModel serviceModel)
    {
        var userId = GetAuthenticatedUserId();


        if (!ModelState.IsValid)
            return BadRequest(ModelState);


        var service = await _serviceService.CreateServiceAsync(serviceModel, userId);

        try
        {
            return CreatedAtAction(nameof(GetServices), new { ServiceId = service.Id }, service);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }

    }

    [HttpPut("edit-service/:{id}")]
    public async Task<IActionResult> UpdateService(int id, [FromForm] ServiceViewModel serviceModel)
    {

        serviceModel.ServiceId = id;
        var result = await _serviceService.UpdateServiceAsync(serviceModel);
        if (!result)
            return NotFound("Service not found");

        return NoContent();
    }

    [HttpDelete("delete:{id}")]
    public async Task<IActionResult> DeleteService(int id)
    {
        var success = await _serviceService.DeleteServiceAsync(id);
        if (!success)
            return NotFound("Service not found");

        return NoContent();
    }


    private string? GetAuthenticatedUserId()
    {
        var token = Request.Headers["Authorization"].ToString();
        var principal = JwtHelper.GetPrincipalFromToken(token, _configuration["Jwt:SecretKey"]);


        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userId == null)
            return Unauthorized("User ID not found.").ToString();
        return userId;
    }
}