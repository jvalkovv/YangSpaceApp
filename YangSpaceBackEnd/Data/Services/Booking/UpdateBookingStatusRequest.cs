using static YangSpaceBackEnd.Data.Extension.Enum;
using System.ComponentModel.DataAnnotations;

namespace YangSpaceBackEnd.Data.Services.Booking
{
    public class UpdateBookingStatusRequest
    {
        [Required]
        public BookingStatus Status { get; set; }
}
    }
