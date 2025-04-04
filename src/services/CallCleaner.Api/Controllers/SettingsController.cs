using CallCleaner.Application.Dtos.Settings; // Bu genel using kalabilir
using CallCleaner.Application.Services; // ISettingsService için eklendi
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    // Helper to get string userId, returns null if not found
    private string GetUserIdString() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    [HttpGet]
    public async Task<IActionResult> GetSettings()
    {
        var userId = GetUserIdString();
        if (userId == null)
            return Unauthorized(new { error = "Invalid token." });

        // Assuming service returns the settings DTO directly or null
        var settings = await _settingsService.GetSettingsAsync(userId);
        if (settings == null)
        {
            return NotFound(new { error = "Settings not found for this user." });
        }
        // Assume 'settings' object matches the spec structure
        return Ok(settings);
    }

    [HttpPut("blocking-mode")]
    public async Task<IActionResult> UpdateBlockingMode([FromBody] UpdateBlockingModeRequestDTO model)
    {
        var userId = GetUserIdString();
        if (userId == null)
            return Unauthorized(new { error = "Invalid token." });

        // Basic validation for the request DTO itself
        if (model == null || string.IsNullOrWhiteSpace(model.Mode))
            return BadRequest(new { error = "Invalid blocking mode specified." });

        // Assuming service returns an object indicating success, e.g., ApiResponseDTO
        var result = await _settingsService.UpdateBlockingModeAsync(userId, model);

        // Check if the result object indicates success (adjust property name if needed)
        if (result == null || !result.Success)
        {
            // Use spec error format
            return BadRequest(new { error = "Invalid blocking mode specified." }); // Or a more general failure message
        }
        // Use spec success format
        return Ok(new { message = "Blocking mode updated successfully." });
    }

    [HttpPut("working-hours")]
    public async Task<IActionResult> UpdateWorkingHours([FromBody] UpdateWorkingHoursRequestDTO model)
    {
        var userId = GetUserIdString();
        if (userId == null)
            return Unauthorized(new { error = "Invalid token." });

        // Basic validation (service should handle complex logic)
        if (model == null || string.IsNullOrWhiteSpace(model.Mode))
            return BadRequest(new { error = "Working hours mode is required." });
        if (model.Mode.ToLower() == "custom" && (string.IsNullOrWhiteSpace(model.StartTime) || string.IsNullOrWhiteSpace(model.EndTime)))
            return BadRequest(new { error = "Invalid time format or missing times for custom mode." });

        var result = await _settingsService.UpdateWorkingHoursAsync(userId, model);

        if (result == null || !result.Success)
        {
            return BadRequest(new { error = "Invalid time format or missing times for custom mode." }); // Or a general failure message
        }
        return Ok(new { message = "Working hours updated successfully." });
    }

    [HttpPut("notifications")]
    public async Task<IActionResult> UpdateNotifications([FromBody] UpdateNotificationsRequestDTO model)
    {
        var userId = GetUserIdString();
        if (userId == null)
            return Unauthorized(new { error = "Invalid token." });

        if (model == null)
            return BadRequest(new { error = "Invalid notification settings data." });

        var result = await _settingsService.UpdateNotificationsAsync(userId, model);

        if (result == null || !result.Success)
        {
            return BadRequest(new { error = "Failed to update notification settings." });
        }
        return Ok(new { message = "Notification settings updated successfully." });
    }

    [HttpGet("whitelist")]
    public async Task<IActionResult> GetWhitelist()
    {
        var userId = GetUserIdString();
        if (userId == null)
            return Unauthorized(new { error = "Invalid token." });

        var whitelistResult = await _settingsService.GetWhitelistAsync(userId);
        return Ok(whitelistResult ?? new List<WhitelistItemDTO>());
    }

    [HttpPost("whitelist")]
    public async Task<IActionResult> AddToWhitelist([FromBody] AddToWhitelistRequestDTO model)
    {
        var userId = GetUserIdString();
        if (userId == null)
            return Unauthorized(new { error = "Invalid token." });

        if (model == null || string.IsNullOrWhiteSpace(model.Number))
            return BadRequest(new { error = "Number is required." });

        // Assuming service returns ApiResponseDTO or similar, potentially indicating conflict
        var result = await _settingsService.AddToWhitelistAsync(userId, model);

        // Need to determine how conflict is indicated by the service.
        // Example: Check a specific error code/message in result if !result.Success
        // For now, assume !result.Success without specific conflict info means BadRequest

        if (result == null) // Handle null result as error
            return BadRequest(new { error = "Failed to add number to whitelist." });

        if (!result.Success)
        {
            // Check for conflict specifically if possible (e.g., based on result.Message or result.ErrorCode)
            // if (result.ErrorCode == "Conflict") // Hypothetical check
            //    return Conflict(new { error = "Number already exists in the whitelist." });

            // Default to Conflict based on spec for this endpoint's error case
            return Conflict(new { error = "Number already exists in the whitelist." });
        }

        // Use spec success format (201 Created)
        return StatusCode(StatusCodes.Status201Created, new { message = "Number added to whitelist successfully." });
    }

    [HttpDelete("whitelist/{number}")]
    public async Task<IActionResult> RemoveFromWhitelist(string number)
    {
        var userId = GetUserIdString();
        if (userId == null)
            return Unauthorized(new { error = "Invalid token." });

        if (string.IsNullOrWhiteSpace(number))
            return BadRequest(new { error = "Number parameter is required." });

        // Assuming service returns ApiResponseDTO or similar, indicating NotFound
        var result = await _settingsService.RemoveFromWhitelistAsync(userId, number);

        // Need to determine how NotFound is indicated.
        // Example: Check a specific error code/message or status
        // For now, assume !result.Success means NotFound for this endpoint's error case

        if (result == null) // Handle null result as error
            return BadRequest(new { error = "Failed to remove number from whitelist." });

        if (!result.Success)
        {
            // if (result.ErrorCode == "NotFound") // Hypothetical check
            //     return NotFound(new { error = "Number not found in the whitelist." });

            // Default to NotFound based on spec for this endpoint's error case
            return NotFound(new { error = "Number not found in the whitelist." });
        }

        // Use spec success format
        return Ok(new { message = "Number removed from whitelist successfully." });
    }
}