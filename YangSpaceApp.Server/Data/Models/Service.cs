using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YangSpaceApp.Server.Data.Models
{
    public class Service
    {
        [Key]
        public int Id { get; set; }

        [Comment("Title of the Service")]
        [Required]
        public string Title { get; set; } = string.Empty;

        [Comment("Description of the Service")]
        public string? Description { get; set; }

        [Comment("Price of the Service")]
        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }

        public DateTime CreatedAt { get; set; }

        public string? ProviderId { get; set; }

        [ForeignKey(nameof(ProviderId))]
        public User Provider { get; set; } = null!;

        public int CategoryId { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public Category Category { get; set; } = null!;

        // Navigation Properties
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();

        // Add this navigation property for images
        public ICollection<ServiceImage> ServiceImages { get; set; } = new List<ServiceImage>();

    }
}