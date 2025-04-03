using System.Collections.Generic;
using CallCleaner.Application.Dtos.Sync;

namespace CallCleaner.Application.Dtos.Sync
{
    public class SyncBlockedNumbersRequestDTO
    {
        public List<BlockedNumberSyncDTO> Numbers { get; set; }
    }

    public class SyncBlockedNumbersResponseDTO
    {
        public int SyncedCount { get; set; }
        public string Message { get; set; }
    }
} 