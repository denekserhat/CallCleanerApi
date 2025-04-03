namespace CallCleaner.Core.Dtos.Auth
{
    public class Token
    {
        public string AccessToken { get; set; }
        public DateTime Expiration { get; set; }
        public string RefreshToken { get; set; }
    }

    public class JustRefreshToken
    {
        public string RefreshToken { get; set; }
    }

    public class VerifyResetTokenRequest
    {
        public string ResetToken { get; set; }
        public string UserId { get; set; }
    }

    public class VerifyResetTokenResponse
    {
        public bool State { get; set; }
    }
}
