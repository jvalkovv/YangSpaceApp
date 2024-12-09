using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace YangSpaceApp.Server.Data.Models
{
    public class ServiceImage
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public int ServiceId { get; set; }

        [ForeignKey(nameof(ServiceId))]
        [JsonIgnore]
        public Service Service { get; set; }
    }
}
