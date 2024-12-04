using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using YangSpaceBackEnd.Data.Services;
using YangSpaceBackEnd.Data.Services.Contracts;
using YangSpaceBackEnd.Data.ViewModel;
using static YangSpaceBackEnd.Data.Extension.Enum;

namespace YangSpaceBackEnd.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly IMapper _mapper;
    private readonly IUserProfileService _userProfileService;

    public BookingController(IBookingService bookingService, IMapper mapper, IUserProfileService userProfileService)
    {
        _bookingService = bookingService;
        _mapper = mapper;
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

        var resultDto = _mapper.Map<BookingViewModel>(result);
        return CreatedAtAction(nameof(GetBookingById), new { id = resultDto.Id }, resultDto);
    }

    [HttpGet("booking:{id}")]
    public async Task<IActionResult> GetBookingById(int id)
    {
        var booking = await _bookingService.GetBookingByIdAsync(id);
        if (booking == null)
            return NotFound();

        var bookingDto = _mapper.Map<BookingViewModel>(booking);
        return Ok(bookingDto);
    }

    [HttpGet("user")]
    public async Task<IActionResult> GetUserBookings()
    {
        var userId = GetUserIdFromToken();
        var userBookings = await _bookingService.GetUserBookingsAsync(userId);
        var userBookingsDto = _mapper.Map<List<BookingViewModel>>(userBookings);
        return Ok(userBookingsDto);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateBookingStatus(int id, [FromBody] UpdateBookingStatusRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { Error = "Invalid status update" });

        var result = await _bookingService.UpdateBookingStatusAsync(id, request);
        if (result == null)
            return NotFound();

        var resultDto = _mapper.Map<BookingViewModel>(result);
        return Ok(resultDto);
    }

    [HttpGet]
    public async Task<IActionResult> GetBookings([FromQuery] BookingStatus? status = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var bookings = await _bookingService.GetBookingsAsync(status, page, pageSize);
        var bookingsDto = _mapper.Map<List<BookingViewModel>>(bookings);
        return Ok(bookingsDto);
    }

    private string? GetUserIdFromToken()
    {
        var userToken = Request.Headers["Authorization"].ToString();
        var principal = _userProfileService.GetUserProfileAsyncByToken(userToken);

        return principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}