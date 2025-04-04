using CallCleaner.Application.Dtos.Core;
using CallCleaner.Application.Dtos.Settings;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CallCleaner.Application.Services;

public interface ISettingsService
{
    // userId parametresi, ayarların hangi kullanıcıya ait olduğunu belirtmek için eklendi.
    Task<GetSettingsResponseDTO> GetSettingsAsync(string userId);
    Task<ApiResponseDTO<object>> UpdateBlockingModeAsync(string userId, UpdateBlockingModeRequestDTO model);
    Task<ApiResponseDTO<object>> UpdateWorkingHoursAsync(string userId, UpdateWorkingHoursRequestDTO model);
    Task<ApiResponseDTO<object>> UpdateNotificationsAsync(string userId, UpdateNotificationsRequestDTO model);
    Task<GetWhitelistResponseDTO> GetWhitelistAsync(string userId);
    Task<ApiResponseDTO<object>> AddToWhitelistAsync(string userId, AddToWhitelistRequestDTO model);
    Task<ApiResponseDTO<object>> RemoveFromWhitelistAsync(string userId, string number);
} 