using CallCleaner.Application.Dtos.Sync; // Tahmini DTO namespace
using CallCleaner.Application.Dtos.Core;
using System.Threading.Tasks;

namespace CallCleaner.Application.Services;

public interface ISyncService
{
    // userId eklendi
    Task<GetLastSyncUpdateResponseDTO> GetLastUpdateTimestampsAsync(string userId);
    Task<SyncBlockedNumbersResponseDTO> SyncBlockedNumbersAsync(string userId, SyncBlockedNumbersRequestDTO model);
    Task<SyncSettingsResponseDTO> SyncSettingsAsync(string userId, SyncSettingsRequestDTO model);
} 