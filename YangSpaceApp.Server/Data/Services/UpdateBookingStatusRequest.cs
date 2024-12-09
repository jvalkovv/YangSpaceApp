using System.ComponentModel.DataAnnotations;
using static YangSpaceApp.Server.Data.Extension.Enum;

namespace YangSpaceApp.Server.Data.Services
{
    public class UpdateBookingStatusRequest
    {
        [Required]
        public BookingStatus Status { get; set; }
    }
}
