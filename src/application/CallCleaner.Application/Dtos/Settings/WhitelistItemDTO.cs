using System;

namespace CallCleaner.Application.Dtos.Settings
{
    public class WhitelistItemDTO
    {
        public string Number { get; set; }
        public string? Name { get; set; } // Opsiyonel
        public DateTime AddedAt { get; set; }
    }
} 