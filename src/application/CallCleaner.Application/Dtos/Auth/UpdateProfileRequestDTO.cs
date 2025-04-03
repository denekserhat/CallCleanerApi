namespace CallCleaner.Application.Dtos.Auth
{
    public class UpdateProfileRequestDTO
    {
        public string Name { get; set; }
        public string? NewPassword { get; set; } // Opsiyonel
    }
} 