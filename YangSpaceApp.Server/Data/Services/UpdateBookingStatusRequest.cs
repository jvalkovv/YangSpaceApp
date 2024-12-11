using System.ComponentModel.DataAnnotations;

namespace YangSpaceApp.Server.Data.Services
{
    public class UpdateBookingStatusRequest
    {
        [Required]
        public string? Status { get; set; }
        public DateTime? ResolvedDate { get; set; }
    }
}
