using System;

namespace CallCleaner.Application.Dtos.Core
{
    public class WorkingHoursDTO
    {
        public string Mode { get; set; } // "24/7", "custom"
        public string? StartTime { get; set; } // Eğer mode 'custom' ise
        public string? EndTime { get; set; }   // Eğer mode 'custom' ise
    }
} 