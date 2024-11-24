using System.ComponentModel.DataAnnotations;

namespace YangSpaceBackEnd.Data.Services.Booking
{
    public class CreateBookingRequest
    {
        [Required]
        public int ServiceId { get; set; }

        [Required]
        public string? Notes { get; set; }
    }
}
