using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YangSpaceApp.Server.Data.Models
{
    public class Review
    {
        [Key]
        public int Id { get; set; }

        public int ServiceId { get; set; }

        [ForeignKey(nameof(ServiceId))]
        public Service Service { get; set; } = null!;

        public string? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        [Comment("Rating (1-5)")]
        [Range(1, 5)]
        public int Rating { get; set; }

        [Comment("Review Comment")]
        public string? Comment { get; set; }

        [Comment("Date of the Review")]
        public DateTime ReviewDate { get; set; } = DateTime.Now;
    }
}