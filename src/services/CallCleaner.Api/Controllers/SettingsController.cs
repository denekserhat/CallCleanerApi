using CallCleaner.Application.Dtos.Core;
using CallCleaner.Application.Dtos.Settings; // Bu genel using kalabilir
using CallCleaner.Application.Services; // ISettingsService için eklendi
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic; // List<> için eklendi
using System.Linq; // SelectMany için eklendi
using System.Security.Claims; // UserId almak için eklendi

// DTO'ların tam konumlarını belirtmek için ek using'ler (veya kod içinde tam isimleri kullan)
// using CallCleaner.Application.Dtos.Settings.GetSettingsResponseDTO;
// using CallCleaner.Application.Dtos.Settings.UpdateBlockingModeDTOs;
// ... (Gerekirse diğerleri)

namespace CallCleaner.Api.Controllers;

[Produces("application/json")]
[Consumes("application/json")]
[ApiController]
[Route("api/settings")]
[Authorize] // Bu controller'daki tüm endpointler yetkilendirme gerektiriyor
public class SettingsController : ControllerBase
{
    private readonly ISettingsService _settingsService;

    // Constructor enjeksiyonu eklendi
    public SettingsController(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet]
    public async Task<IActionResult> GetSettings()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized(); // Veya hata mesajı ile

        var settings = await _settingsService.GetSettingsAsync(userId);
        if (settings == null)
        {
            return NotFound(new ApiResponseDTO<object> { Success = false, Message = "Settings not found for this user." });
        }
        return Ok(settings); // GetSettingsResponseDTO döndürür
    }

    [HttpPut("blocking-mode")]
    public async Task<IActionResult> UpdateBlockingMode([FromBody] UpdateBlockingModeRequestDTO model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        if (!ModelState.IsValid) return BadRequest(new ApiResponseDTO<object> { Success = false, Message = "Invalid input", Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });

        var result = await _settingsService.UpdateBlockingModeAsync(userId, model);
        if (!result.Success)
        {
            // Servis Not Found veya başka bir hata döndürdüyse
            // TODO: result.IsNotFound gibi flag'ler varsa ona göre NotFound() dönülebilir.
            // Şimdilik genel BadRequest dönüyoruz.
            return BadRequest(result); // ApiResponseDTO<object> (Success=false içerir)
        }
        return Ok(result); // ApiResponseDTO<object> (Success=true içerir)
    }

    [HttpPut("working-hours")]
    public async Task<IActionResult> UpdateWorkingHours([FromBody] UpdateWorkingHoursRequestDTO model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        if (!ModelState.IsValid) return BadRequest(new ApiResponseDTO<object> { Success = false, Message = "Invalid input", Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });

        var result = await _settingsService.UpdateWorkingHoursAsync(userId, model);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPut("notifications")]
    public async Task<IActionResult> UpdateNotifications([FromBody] UpdateNotificationsRequestDTO model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        if (!ModelState.IsValid) return BadRequest(new ApiResponseDTO<object> { Success = false, Message = "Invalid input", Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });

        var result = await _settingsService.UpdateNotificationsAsync(userId, model);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpGet("whitelist")]
    public async Task<IActionResult> GetWhitelist()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var whitelist = await _settingsService.GetWhitelistAsync(userId);
        // GetWhitelistResponseDTO doğrudan List<WhitelistItemDTO>'dan miras aldığı için null kontrolü gerekmeyebilir
        // Ancak servis null döndürebilir mi kontrol edilmeli.
        return Ok(whitelist); // GetWhitelistResponseDTO (List<WhitelistItemDTO>) döndürür
    }

    [HttpPost("whitelist")]
    public async Task<IActionResult> AddToWhitelist([FromBody] AddToWhitelistRequestDTO model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        if (!ModelState.IsValid) return BadRequest(new ApiResponseDTO<object> { Success = false, Message = "Invalid input", Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });

        var result = await _settingsService.AddToWhitelistAsync(userId, model);
        if (!result.Success)
        {
            // TODO: result.IsConflict gibi flag'ler varsa Conflict() dönülebilir.
            return Conflict(result); // Veya BadRequest(result)
        }
        // Başarılı eklemede 201 Created dönmek daha uygun
        return CreatedAtAction(nameof(GetWhitelist), result); // Veya sadece Ok(result)
    }

    [HttpDelete("whitelist/{number}")]
    public async Task<IActionResult> RemoveFromWhitelist(string number)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(number)) return BadRequest("Number cannot be empty.");

        var result = await _settingsService.RemoveFromWhitelistAsync(userId, number);
        if (!result.Success)
        {
            // TODO: result.IsNotFound gibi flag'ler varsa NotFound() dönülebilir.
            return NotFound(result); // Veya BadRequest(result)
        }
        return Ok(result);
    }
} 