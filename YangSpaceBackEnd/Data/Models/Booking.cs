using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using static YangSpaceBackEnd.Data.Extension.Enum;

namespace YangSpaceBackEnd.Data.Models
{
    public class Booking
    {
        [Key]
        public int Id { get; set; }

        public int ServiceId { get; set; }

        [ForeignKey(nameof(ServiceId))]
        public Service Service { get; set; } = null!;

        public string? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        [Comment("Date of the Booking")]
        [Required]
        public DateTime BookingDate { get; set; }

        [Comment("Status of the Booking (Pending/Confirmed/Cancelled)")]
        [Required]
        public BookingStatus Status { get; set; }

        public bool IsCompleted { get; set; }
    }
}