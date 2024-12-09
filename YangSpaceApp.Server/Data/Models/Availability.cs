using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YangSpaceApp.Server.Data.Models
{
    public class Availability
    {
        [Key]
        public int Id { get; set; }

        public string? ProviderId { get; set; }

        [ForeignKey(nameof(ProviderId))]
        public User Provider { get; set; } = null!;

        [Comment("Day of the Week")]
        [Required]
        public DayOfWeek DayOfWeek { get; set; }

        [Comment("Start Time")]
        [Required]
        public TimeSpan StartTime { get; set; }

        [Comment("End Time")]
        [Required]
        public TimeSpan EndTime { get; set; }
    }
}