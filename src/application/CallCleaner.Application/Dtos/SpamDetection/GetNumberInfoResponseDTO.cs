using System;
using System.Collections.Generic;
using CallCleaner.Application.Dtos.SpamDetection; // NumberCommentDTO için

namespace CallCleaner.Application.Dtos.SpamDetection
{
    // Request DTO yok (Token header'da, numara URL'de)

    public class GetNumberInfoResponseDTO
    {
        public string PhoneNumber { get; set; }
        public bool IsSpam { get; set; }
        public string? SpamType { get; set; }
        public int ReportCount { get; set; }
        public DateTime? FirstReported { get; set; }
        public DateTime? LastReported { get; set; }
        public List<NumberCommentDTO>? Comments { get; set; } // Opsiyonel
    }
} 