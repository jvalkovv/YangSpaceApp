using System.ComponentModel.DataAnnotations;
using static YangSpaceBackEnd.Data.Extension.Enum;

namespace YangSpaceBackEnd.Data.Services1.Booking
{
    public class CreateBookingRequest
    {
        public int Id { get; set; }
        public int ServiceId { get; set; }
        public string UserId { get; set; }  // This will refer to the user booking the service
        public DateTime RequestedDate { get; set; }  // The date the user wants to book the service
        public BookingStatus Status { get; set; } = BookingStatus.Pending;// Default status is Pending

        [Required] 
        public string? Notes { get; set; }
    }
}
