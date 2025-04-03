namespace CallCleaner.Application.Dtos.Settings
{
    public class UpdateBlockingModeRequestDTO
    {
        public string Mode { get; set; } // "all", "known", "custom"
    }

    public class UpdateBlockingModeResponseDTO
    {
        public string Message { get; set; }
    }
} 