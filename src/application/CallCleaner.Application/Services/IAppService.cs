using CallCleaner.Application.Dtos.App;

namespace CallCleaner.Application.Services;

public interface IAppService
{
    Task<GetAppVersionResponseDTO> GetAppVersionAsync();
    Task<List<PermissionDTO>> GetRequiredPermissionsAsync();
    Task<VerifyPermissionsResponseDTO> VerifyPermissionsAsync(int userId, VerifyPermissionsRequestDTO model);
    Task<GetPrivacyPolicyResponseDTO> GetPrivacyPolicyAsync();
}

public class AppService : IAppService
{

    // TODO: Versiyon bilgileri appsettings den alınacak
    public async Task<GetAppVersionResponseDTO> GetAppVersionAsync()
    {
        await Task.CompletedTask;
        return new GetAppVersionResponseDTO
        {
            LatestVersion = "1.2.0",
            MinRequiredVersion = "1.1.0",
            UpdateUrl = "market://details?id=com.callcleaner"
        };
    }

    // TODO: Gerekli izinleri appsettings den al.
    public async Task<List<PermissionDTO>> GetRequiredPermissionsAsync()
    {
        await Task.CompletedTask;
        return new List<PermissionDTO>
        {
            new PermissionDTO { Id = "READ_CALL_LOG", Reason = "Gelen aramaları tespit etmek için." },
            new PermissionDTO { Id = "READ_PHONE_STATE", Reason = "Arama durumunu takip etmek için." },
            new PermissionDTO { Id = "ANSWER_PHONE_CALLS", Reason = "Aramaları engellemek için." }
        };
    }
    // TODO: Gerekli izinler için doğrulama mantığını uygula
    public async Task<VerifyPermissionsResponseDTO> VerifyPermissionsAsync(int userId, VerifyPermissionsRequestDTO model)
    {
        await Task.CompletedTask;

        var required = await GetRequiredPermissionsAsync();
        var grantedSet = new HashSet<string>(model.GrantedPermissions ?? new List<string>());
        var missing = required.Where(p => !grantedSet.Contains(p.Id)).Select(p => p.Id).ToList();
        string status = missing.Count == 0 ? "ok" : (missing.Count < required.Count ? "partial" : "missing");

        return new VerifyPermissionsResponseDTO
        {
            Status = status,
            Missing = missing
        };
    }

    // TODO: Gizlilik politikası bilgilerini appsettings den al
    public async Task<GetPrivacyPolicyResponseDTO> GetPrivacyPolicyAsync()
    {
        await Task.CompletedTask;
        Console.WriteLine("Gizlilik politikası bilgileri alınıyor");
        return new GetPrivacyPolicyResponseDTO
        {
            Url = "https://example.com/privacy",
            LastUpdated = new DateTime(2023, 3, 15)
        };
    }
}