using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using static YangSpaceApp.Server.Data.Extension.Enum;

namespace YangSpaceApp.Server.Data.Models
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

        [Comment("Status of the Booking (Pending/InProgress/Completed/Cancelled)")]
        [Required]
        public BookingStatus Status { get; set; }

        [Comment("Optional Notes for the Booking")]
        public string? Notes { get; set; }

        [Comment("Last Updated Date for the Booking")]
        public DateTime? UpdatedDate { get; set; }
    }
}