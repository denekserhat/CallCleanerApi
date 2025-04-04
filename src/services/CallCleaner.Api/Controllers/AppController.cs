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
    [HttpGet("version")]
    public async Task<IActionResult> GetAppVersion()
    {
        var versionInfo = await _appService.GetAppVersionAsync();
        return Ok(versionInfo);
    }

    [HttpGet("required-permissions")]
    public async Task<IActionResult> GetRequiredPermissions()
    {
        var permissions = await _appService.GetRequiredPermissionsAsync();
        return Ok(permissions);
    }

    [HttpPost("verify-permissions")]
    [Authorize]
    public async Task<IActionResult> VerifyPermissions([FromBody] VerifyPermissionsRequestDTO model)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdString == null)
        {
            return Unauthorized(new ApiResponseDTO<object> { Success = false, Message = "Invalid token: User ID not found." });
        }

        if (!int.TryParse(userIdString, out var userId))
        {
            return Unauthorized(new ApiResponseDTO<object> { Success = false, Message = "Invalid token: User ID format is incorrect." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiResponseDTO<object> { Success = false, Message = "Invalid input", Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });
        }

        var verificationResult = await _appService.VerifyPermissionsAsync(userId, model);
        return Ok(verificationResult);
    }

    [HttpGet("privacy-policy")]
    public async Task<IActionResult> GetPrivacyPolicy()
    {
        var policyInfo = await _appService.GetPrivacyPolicyAsync();
        return Ok(policyInfo);
    }
}