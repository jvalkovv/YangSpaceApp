using System.ComponentModel.DataAnnotations;
using static YangSpaceBackEnd.Data.Extension.Enum;

namespace YangSpaceBackEnd.Data.Services
{
    public class UpdateBookingStatusRequest
    {
        [Required]
        public BookingStatus Status { get; set; }
    }
}
