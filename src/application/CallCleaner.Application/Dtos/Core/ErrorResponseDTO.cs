using System;

namespace CallCleaner.Application.Dtos.Core
{
    public class ErrorResponseDTO
    {
        public string Error { get; set; }
        public string? Details { get; set; } // Opsiyonel
    }
} 