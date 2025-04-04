using CallCleaner.Application.Dtos.App;
using CallCleaner.Application.Dtos.Core;
using CallCleaner.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CallCleaner.Api.Controllers;

[Produces("application/json")]
[Consumes("application/json")]
[ApiController]
[Route("api/app")]
public class AppController : ControllerBase
{
    private readonly IAppService _appService;
    public AppController(IAppService appService)
    {
        _appService = appService;
    }

    // TODO: Gerekli servisleri veya konfigürasyonu inject et
    // public AppController(IConfiguration config) { ... }

    [HttpGet("version")]
    // DTO ismi tahmin ediliyor: AppVersionInfoDTO
    public async Task<IActionResult> GetAppVersion()
    {
        var versionInfo = await _appService.GetAppVersionAsync();
        return Ok(versionInfo); // DTO doğrudan döndürülüyor
    }

    [HttpGet("required-permissions")]
    // DTO ismi tahmin ediliyor: List<RequiredPermissionDTO>
    public async Task<IActionResult> GetRequiredPermissions()
    {
        var permissions = await _appService.GetRequiredPermissionsAsync();
        return Ok(permissions); // List<PermissionDTO> doğrudan döndürülüyor
    }

    [HttpPost("verify-permissions")]
    [Authorize] // Kullanıcının izinlerini doğrulamak yetkilendirme gerektirir
    // DTO isimleri tahmin ediliyor: VerifyPermissionsRequestDTO, VerifyPermissionsResponseDTO
    public async Task<IActionResult> VerifyPermissions([FromBody] VerifyPermissionsRequestDTO model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized(new ApiResponseDTO<object> { Success = false, Message = "Invalid token: User ID not found." });
        }

        // ModelState kontrolü burada gereksiz olabilir çünkü servis hata döndürmeyecek,
        // ancak API tutarlılığı için kalabilir.
        if (!ModelState.IsValid)
            return BadRequest(new ApiResponseDTO<object> { Success = false, Message = "Invalid input", Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });

        var verificationResult = await _appService.VerifyPermissionsAsync(userId, model);
        return Ok(verificationResult); // DTO doğrudan döndürülüyor
    }

    // Gizlilik politikası endpoint'i ayrı bir controller'da (örn. LegalController) veya burada olabilir.
    // Şimdilik buraya ekliyorum.
    [HttpGet("/api/privacy-policy")] // Route tam olarak belirtildi
    // DTO ismi tahmin ediliyor: PrivacyPolicyInfoDTO
    public async Task<IActionResult> GetPrivacyPolicy()
    {
        var policyInfo = await _appService.GetPrivacyPolicyAsync();
        return Ok(policyInfo); // DTO doğrudan döndürülüyor
    }
}