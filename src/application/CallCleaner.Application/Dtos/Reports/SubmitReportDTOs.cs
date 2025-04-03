using System;

namespace CallCleaner.Application.Dtos.Reports
{
    public class SubmitReportRequestDTO
    {
        public string PhoneNumber { get; set; }
        public string SpamType { get; set; } // "telemarketing", "scam", "annoying", "other"
        public string? Description { get; set; } // Opsiyonel
        public DateTime? Timestamp { get; set; } // Opsiyonel
    }

    public class SubmitReportResponseDTO
    {
        public string ReportId { get; set; }
        public string Message { get; set; }
    }
} 