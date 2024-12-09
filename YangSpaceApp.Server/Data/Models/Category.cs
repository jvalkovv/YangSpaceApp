using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace YangSpaceApp.Server.Data.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Comment("Name of the Category")]
        [Required]
        public string Name { get; set; } = string.Empty;

        [Comment("Description of the Category")]
        public string? Description { get; set; }

        // Navigation Properties
        public ICollection<Service> Services { get; set; } = new List<Service>();
    }
}