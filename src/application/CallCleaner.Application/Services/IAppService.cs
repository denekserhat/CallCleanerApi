using CallCleaner.Application.Dtos.App;
using CallCleaner.Application.Dtos.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CallCleaner.Application.Services;

public interface IAppService
{
    // DTO isimleri güncellendi
    Task<GetAppVersionResponseDTO> GetAppVersionAsync();
    // DTO ismi PermissionDTO olarak düzeltildi
    Task<List<PermissionDTO>> GetRequiredPermissionsAsync();
    // VerifyPermissions userId gerektirir
    Task<VerifyPermissionsResponseDTO> VerifyPermissionsAsync(string userId, VerifyPermissionsRequestDTO model);
    // DTO ismi tahmin edildi: GetPrivacyPolicyResponseDTO
    Task<GetPrivacyPolicyResponseDTO> GetPrivacyPolicyAsync();
} 