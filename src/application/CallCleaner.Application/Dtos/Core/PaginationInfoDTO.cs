using System;

namespace CallCleaner.Application.Dtos.Core
{
    public class PaginationInfoDTO
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
    }
} 