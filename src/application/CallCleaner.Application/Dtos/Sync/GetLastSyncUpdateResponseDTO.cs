using System;

namespace CallCleaner.Application.Dtos.Sync
{
    // Request DTO yok (Token header'da)

    public class GetLastSyncUpdateResponseDTO
    {
        public DateTime SettingsTimestamp { get; set; }
        public DateTime BlockedNumbersTimestamp { get; set; }
    }
} 