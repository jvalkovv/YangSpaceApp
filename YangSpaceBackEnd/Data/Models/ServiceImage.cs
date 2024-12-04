using System.ComponentModel.DataAnnotations.Schema;

namespace YangSpaceBackEnd.Data.Models
{
    public class ServiceImage
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public int ServiceId { get; set; }

        [ForeignKey(nameof(ServiceId))]
        public Service? Service { get; set; }
    }
}
