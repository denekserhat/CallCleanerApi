using CallCleaner.Application.Dtos.Sync; // Varsayılan
using CallCleaner.Application.Services; // Assuming ISyncService
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CallCleaner.Api.Controllers;

[Produces("application/json")]
[Consumes("application/json")]
[ApiController]
[Route("api/sync")]
[Authorize] // Bu controller'daki tüm endpointler yetkilendirme gerektiriyor
public class SyncController : ControllerBase
{
    private readonly ISyncService _syncService; // Assuming service interface

    public SyncController(ISyncService syncService)
    {
        _syncService = syncService;
    }

    private string GetUserIdString() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    [HttpGet("last-update")]
    // DTO ismi tahmin ediliyor: GetLastUpdateTimestampsResponseDTO
    public async Task<IActionResult> GetLastUpdateTimestamps()
    {
        var userId = GetUserIdString();
        if (userId == null)
            return Unauthorized(new { error = "Invalid token." });

        // Assuming service returns { SettingsTimestamp, BlockedNumbersTimestamp } or null
        var timestamps = await _syncService.GetLastUpdateTimestampsAsync(userId);

        if (timestamps == null)
        {
            // Return default/null timestamps if service returns null
            return Ok(new { settingsTimestamp = (DateTime?)null, blockedNumbersTimestamp = (DateTime?)null });
        }

        // Return timestamps from service
        return Ok(timestamps);
    }

    [HttpPost("blocked-numbers")]
    // DTO isimleri tahmin ediliyor: SyncBlockedNumbersRequestDTO, SyncBlockedNumbersResponseDTO
    public async Task<IActionResult> SyncBlockedNumbers([FromBody] SyncBlockedNumbersRequestDTO model)
    {
        var userId = GetUserIdString();
        if (userId == null)
            return Unauthorized(new { error = "Invalid token." });

        // Basic validation
        if (model == null || model.Numbers == null) // Check if Numbers list exists
            return BadRequest(new { error = "Invalid request format." });

        // Optional: Validate individual items if needed
        // if (model.Numbers.Any(n => string.IsNullOrWhiteSpace(n.PhoneNumber))) 
        //    return BadRequest(new { error = "Invalid phone number in list." });

        // Assuming service returns { Success, SyncedCount } or similar
        var result = await _syncService.SyncBlockedNumbersAsync(userId, model);

        if (result == null)
        {
            return BadRequest(new { error = "Failed to sync blocked numbers." });
        }

        // Use spec success format
        return Ok(new { syncedCount = result.SyncedCount, message = "Blocked numbers synced successfully." });
    }

    [HttpPost("settings")]
    // DTO isimleri tahmin ediliyor: SyncSettingsRequestDTO, SyncSettingsResponseDTO
    public async Task<IActionResult> SyncSettings([FromBody] SyncSettingsRequestDTO model)
    {
        var userId = GetUserIdString();
        if (userId == null)
            return Unauthorized(new { error = "Invalid token." });

        // Basic validation (could add more specific checks)
        if (model == null || string.IsNullOrWhiteSpace(model.BlockingMode))
            return BadRequest(new { error = "Invalid settings data." });
        // Add checks for WorkingHours if needed based on BlockingMode

        // Assuming service returns { Success } or similar
        var result = await _syncService.SyncSettingsAsync(userId, model);

        if (result == null)
        {
            return BadRequest(new { error = "Failed to sync settings." });
        }

        // Use spec success format
        return Ok(new { message = "Settings synced successfully." });
    }
}