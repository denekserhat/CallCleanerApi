namespace CallCleaner.Application.Dtos.User
{
    public class UserReportDTO
    {
        public int Id { get; set; }
        public int ReportedByUserId { get; set; }
        public string ReportedByUsername { get; set; }
        public string Reason { get; set; }
        public string Description { get; set; }
        public DateTime ReportDate { get; set; }
        public string Status { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewNotes { get; set; }
    }
}
