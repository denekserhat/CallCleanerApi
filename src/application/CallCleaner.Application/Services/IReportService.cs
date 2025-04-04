using CallCleaner.Application.Dtos.Reports; // Tahmini DTO namespace
using CallCleaner.Application.Dtos.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CallCleaner.Application.Services;

public interface IReportService
{
    // SubmitReport için userId eklendi
    Task<SubmitReportResponseDTO> SubmitReportAsync(string userId, SubmitReportRequestDTO model);
    // GetRecentCalls için userId ve limit eklendi
    Task<List<RecentCallDTO>> GetRecentCallsAsync(string userId, int limit);
    // GetSpamTypes için parametreye gerek yok (genel bilgi)
    Task<List<SpamTypeDTO>> GetSpamTypesAsync();
} 