namespace CallCleaner.Application.Dtos.Settings
{
    public class AddToWhitelistRequestDTO
    {
        public string Number { get; set; }
        public string? Name { get; set; } // Opsiyonel
    }

    public class AddToWhitelistResponseDTO
    {
        public string Message { get; set; }
    }
}