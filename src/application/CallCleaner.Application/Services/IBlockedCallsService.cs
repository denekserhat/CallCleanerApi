using CallCleaner.Application.Dtos.BlockedCalls; // Tahmini DTO namespace
using CallCleaner.Application.Dtos.Core;
using System.Threading.Tasks;

namespace CallCleaner.Application.Services;

public interface IBlockedCallsService
{
    // GetBlockedCalls için pagination ve userId parametreleri eklendi
    Task<GetBlockedCallsResponseDTO> GetBlockedCallsAsync(string userId, int page, int limit);
    Task<GetBlockedCallsStatsResponseDTO> GetStatsAsync(string userId);
    Task<ApiResponseDTO<object>> DeleteBlockedCallAsync(string userId, string callId);
    Task<ApiResponseDTO<object>> DeleteAllBlockedCallsAsync(string userId);
    Task<ApiResponseDTO<object>> ReportWronglyBlockedAsync(string userId, string callId);
} 