using System;

namespace CallCleaner.Application.Dtos.SpamDetection
{
    public class NumberCommentDTO
    {
        public string User { get; set; }
        public string Comment { get; set; }
        public DateTime Timestamp { get; set; }
    }
} 