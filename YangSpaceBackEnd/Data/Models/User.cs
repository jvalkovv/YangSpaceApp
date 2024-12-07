using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace YangSpaceBackEnd.Data.Models
{
    public class User : IdentityUser
    {
        [Comment("User First Name")]
        [Required]
        public string? FirstName { get; set; } = string.Empty;

        [Comment("User Last Name")]
        [Required]
        public string? LastName { get; set; } = string.Empty;

        [Comment("Role of the User (Admin/Provider/User)")]
        [Required]
        public string Role { get; set; } = string.Empty;

        [Comment("Profile Picture URL")]
        public string? ProfilePictureUrl { get; set; }

        //PhoneNUmber
        [MaxLength(10)]
        public string? PhoneNumber { get; set; }

        [Comment("User Bio/Description")]
        public string? Bio { get; set; }
        public string? Location { get; set; }

        // Navigation Properties
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<Service> Services { get; set; } = new List<Service>();
        public ICollection<Availability> Availabilities { get; set; } = new List<Availability>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}