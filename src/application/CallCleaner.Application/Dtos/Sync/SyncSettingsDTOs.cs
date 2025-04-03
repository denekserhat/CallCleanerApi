using System;
using CallCleaner.Application.Dtos.Core; // WorkingHoursDTO için

namespace CallCleaner.Application.Dtos.Sync
{
    public class SyncSettingsRequestDTO
    {
        public string BlockingMode { get; set; }
        public WorkingHoursDTO WorkingHours { get; set; }
        public bool NotificationsEnabled { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class SyncSettingsResponseDTO
    {
        public string Message { get; set; }
    }
} 