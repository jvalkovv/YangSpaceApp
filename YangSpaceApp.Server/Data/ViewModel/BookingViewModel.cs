﻿using static YangSpaceApp.Server.Data.Extension.Enum;

namespace YangSpaceApp.Server.Data.ViewModel
{
    public class BookingViewModel
    {
        public int Id { get; set; }
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = null!;  // Ensure ServiceName is non-null
        public string? UserId { get; set; }
        public string? UserName { get; set; }

        public string? ClientEmail { get; set; }
        public string? ClientName { get; set; }
        public string? ProviderEmail { get; set; }
        
        public DateTime BookingDate { get; set; }
        public string Status { get; set; }
        public string? Notes { get; set; }
        public DateTime? UpdatedDate { get; set; }

        public decimal Price { get; set; }
    }
}
