using System.Collections.Generic;
using CallCleaner.Application.Dtos.Reports; // RecentCallDTO için

namespace CallCleaner.Application.Dtos.Reports
{
    // Request DTO yok (Token header'da, limit query param)

    public class GetRecentCallsResponseDTO : List<RecentCallDTO>
    {
    }
} 