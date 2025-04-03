namespace CallCleaner.Application.Dtos.App
{
    // Request DTO yok

    public class GetAppVersionResponseDTO
    {
        public string LatestVersion { get; set; }
        public string MinRequiredVersion { get; set; }
        public string UpdateUrl { get; set; }
    }
} 