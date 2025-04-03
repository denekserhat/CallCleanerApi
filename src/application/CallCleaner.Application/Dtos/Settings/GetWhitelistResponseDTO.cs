using System.Collections.Generic;
using CallCleaner.Application.Dtos.Settings; // WhitelistItemDTO için

namespace CallCleaner.Application.Dtos.Settings
{
    // Request DTO yok (Token header'da)

    public class GetWhitelistResponseDTO : List<WhitelistItemDTO>
    {
    }
} 