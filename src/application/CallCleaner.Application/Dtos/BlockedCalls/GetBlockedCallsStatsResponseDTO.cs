namespace CallCleaner.Application.Dtos.BlockedCalls
{
    // Request DTO yok (Token header'da)

    public class GetBlockedCallsStatsResponseDTO
    {
        public int Today { get; set; }
        public int ThisWeek { get; set; }
        public int Total { get; set; }
    }
} 