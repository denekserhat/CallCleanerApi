namespace CallCleaner.Application.Dtos.Auth
{
    // Request DTO yok (Token header'da)

    public class VerifyTokenResponseDTO
    {
        public string UserId { get; set; }
        public bool IsValid { get; set; }
    }
} 