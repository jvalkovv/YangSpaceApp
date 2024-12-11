using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using YangSpaceApp.Server.Data.Services;
using YangSpaceApp.Server.Data.Services.Contracts;
using static YangSpaceApp.Server.Data.Extension.Enum;

namespace YangSpaceApp.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly IUserProfileService _userProfileService;

    public BookingController(IBookingService bookingService, IUserProfileService userProfileService)
    {
        _bookingService = bookingService;

        _userProfileService = userProfileService;
    }

    // Fetch booked services for the provider
    [HttpGet("provider/{providerId}")]
    public async Task<IActionResult> GetBookedServicesForProvider(string providerId)
    {
        var userId = GetUserIdFromToken();
        var bookings = await _bookingService.GetBookingsForProviderAsync(userId);
        if (bookings == null)
        {
            return NotFound();
        }
        return Ok(bookings);
    }

    // Get booking by ID
    [HttpGet("booking/{id}")]
    public async Task<IActionResult> GetBookingById(int id)
    {
        var booking = await _bookingService.GetBookingByIdAsync(id);
        if (booking == null)
            return NotFound();
        return Ok(booking); // Ensure booking is passed to response
    }


    // Get bookings for the current user
    [HttpGet("user")]
    public async Task<IActionResult> GetUserBookings()
    {
        var userId = GetUserIdFromToken();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var userBookings = await _bookingService.GetUserBookingsAsync(userId);
        return Ok(userBookings); 
    }


    [HttpPatch("{bookingId}/status")]
    public async Task<IActionResult> UpdateBookingStatus(int bookingId, [FromBody] UpdateBookingStatusRequest request)
    {
        var success = await _bookingService.UpdateBookingStatusAsync(bookingId, request);
        if (!success)
        {
            return BadRequest("Failed to update booking status.");
        }
        return NoContent();
    }

    // Get all bookings with optional filters for status and pagination
    [HttpGet]
    public async Task<IActionResult> GetBookings([FromQuery] string status = "all", [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        // Validate pagination parameters
        if (page < 1 || pageSize < 1)
        {
            return BadRequest("Page and pageSize must be greater than zero.");
        }

        // If status is "all", treat it as no filter, otherwise filter by enum status
        var bookings = await _bookingService.GetBookingsAsync(status.ToLower(), page, pageSize);
        return Ok(bookings);
    }

    private string? GetUserIdFromToken()
    {
        var userToken = Request.Headers["Authorization"].ToString();
        var principal = _userProfileService.GetUserProfileAsyncByToken(userToken);

        return principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}