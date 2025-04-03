namespace CallCleaner.Application.Dtos.SpamDetection
{
    public class CheckNumberRequestDTO
    {
        public string PhoneNumber { get; set; }
    }

    public class CheckNumberResponseDTO
    {
        public bool IsSpam { get; set; }
        public string? SpamType { get; set; } // Eğer biliniyorsa
        public int RiskScore { get; set; } // 0-100 arası
    }
} 