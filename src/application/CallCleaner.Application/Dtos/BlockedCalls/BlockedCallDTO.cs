using System;

namespace CallCleaner.Application.Dtos.BlockedCalls
{
    public class BlockedCallDTO
    {
        public string Id { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime Timestamp { get; set; }
        public string CallType { get; set; }
    }
} 