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

    [HttpPost]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { Error = "Invalid request" });

        var result = await _bookingService.CreateBookingAsync(request);
        if (result == null)
            return NotFound(new { Error = "Service not found" });

        return null;
    }

    [HttpGet("booking:{id}")]
    public async Task<IActionResult> GetBookingById(int id)
    {
        var booking = await _bookingService.GetBookingByIdAsync(id);
        if (booking == null)
            return NotFound();
        ;
        return Ok();
    }

    [HttpGet("user")]
    public async Task<IActionResult> GetUserBookings()
    {
        var userId = GetUserIdFromToken();
        var userBookings = await _bookingService.GetUserBookingsAsync(userId);
        return Ok();
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateBookingStatus(int id, [FromBody] UpdateBookingStatusRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { Error = "Invalid status update" });

        var result = await _bookingService.UpdateBookingStatusAsync(id, request);
        if (result == null)
            return NotFound();

        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> GetBookings([FromQuery] BookingStatus? status = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var bookings = await _bookingService.GetBookingsAsync(status, page, pageSize);
        return Ok();
    }

    private string? GetUserIdFromToken()
    {
        var userToken = Request.Headers["Authorization"].ToString();
        var principal = _userProfileService.GetUserProfileAsyncByToken(userToken);

        return principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}