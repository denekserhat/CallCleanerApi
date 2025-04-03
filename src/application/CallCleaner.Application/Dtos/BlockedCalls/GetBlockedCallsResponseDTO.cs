using System.Collections.Generic;
using CallCleaner.Application.Dtos.BlockedCalls; // BlockedCallDTO için
using CallCleaner.Application.Dtos.Core; // PaginationInfoDTO için

namespace CallCleaner.Application.Dtos.BlockedCalls
{
    // Request DTO yok (Token header'da, page/limit query params)

    public class GetBlockedCallsResponseDTO
    {
        public List<BlockedCallDTO> Calls { get; set; }
        public PaginationInfoDTO Pagination { get; set; }
    }
} 