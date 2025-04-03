namespace CallCleaner.Application.Dtos.Auth
{
    public class ConfirmMailDto
    {
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// 6 haneli doğrulama kodu
        /// </summary>
        public int? Code { get; set; }
    }
}
