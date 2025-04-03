namespace CallCleaner.Application.Dtos.Settings
{
    public class UpdateNotificationsRequestDTO
    {
        public bool Enabled { get; set; }
    }

    public class UpdateNotificationsResponseDTO
    {
        public string Message { get; set; }
    }
} 