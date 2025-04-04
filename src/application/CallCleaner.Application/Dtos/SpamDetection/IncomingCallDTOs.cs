using System;

namespace CallCleaner.Application.Dtos.SpamDetection;

public class IncomingCallRequestDTO
{
    public string PhoneNumber { get; set; }
    public DateTime Timestamp { get; set; }
}

public class IncomingCallResponseDTO
{
    public string Action { get; set; } // "block", "allow", "warn"
    public string? Reason { get; set; } // Opsiyonel
} 