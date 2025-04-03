namespace CallCleaner.Application.Dtos.Auth
{
    public class Token
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class JustRefreshToken
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class VerifyResetTokenRequest
    {
        public string ResetToken { get; set; } = string.Empty;
        public int UserId { get; set; }
    }

    public class VerifyResetTokenResponse
    {
        public bool State { get; set; }
    }
}
