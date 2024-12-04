using System.ComponentModel.DataAnnotations;
using static YangSpaceBackEnd.Data.Extension.Enum;

namespace YangSpaceBackEnd.Data.Services
{
    public class CreateBookingRequest
    {
        public int Id { get; set; }
        public int ServiceId { get; set; }
        public string? UserTokenKey { get; set; }  
        public DateTime RequestedDate { get; set; }  // The date the user wants to book the service
        public BookingStatus Status { get; set; } = BookingStatus.Pending;// Default status is Pending

        [Required]
        public string? Notes { get; set; }
    }
}
