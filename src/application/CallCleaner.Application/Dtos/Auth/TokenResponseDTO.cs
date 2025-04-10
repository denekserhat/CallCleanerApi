using System.ComponentModel.DataAnnotations;

namespace CallCleaner.Application.Dtos.Auth
{
    public class TokenResponseDTO
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        // İsteğe bağlı olarak kullanıcı bilgileri de eklenebilir
        public int UserId { get; set; }
        public string? FullName { get; set; }
    }

    public class RefreshTokenRequestDTO
    {
        [Required]
        public string? RefreshToken { get; set; }
    }

    public class RevokeTokenRequestDTO
    {
        [Required]
        public string? RefreshToken { get; set; }
    }
}