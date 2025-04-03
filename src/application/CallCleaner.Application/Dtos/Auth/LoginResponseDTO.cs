namespace CallCleaner.Application.Dtos.Auth
{
    public class LoginResponseDTO
    {
        public int UserId { get; set; }
        public string Token { get; set; }
        public string Email { get; set; }
    }
}