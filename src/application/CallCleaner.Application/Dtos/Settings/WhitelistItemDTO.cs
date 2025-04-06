namespace CallCleaner.Application.Dtos.Settings
{
    public class WhitelistItemDTO
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Name { get; set; }
        public DateTime AddedAt { get; set; }
    }
}