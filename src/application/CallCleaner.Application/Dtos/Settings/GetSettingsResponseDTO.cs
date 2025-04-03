using CallCleaner.Application.Dtos.Core; // WorkingHoursDTO için

namespace CallCleaner.Application.Dtos.Settings
{
    // Request DTO yok (Token header'da)

    public class GetSettingsResponseDTO
    {
        public string BlockingMode { get; set; } // "all", "known", "custom"
        public WorkingHoursDTO WorkingHours { get; set; }
        public bool NotificationsEnabled { get; set; }
    }
} 