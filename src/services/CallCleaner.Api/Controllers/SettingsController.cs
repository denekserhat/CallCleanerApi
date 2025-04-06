using CallCleaner.Application.Dtos.Settings;
using CallCleaner.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Http; // For StatusCodes
using System.Collections.Generic; // For List<>

namespace CallCleaner.Api.Controllers;

[Produces("application/json")]
[Consumes("application/json")]
[ApiController]
[Route("api/settings")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly ISettingsService _settingsService;

    public SettingsController(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    private string GetUserIdString() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    [HttpGet]
    public async Task<IActionResult> GetSettings()
    {
        var userId = GetUserIdString();
        if (userId == null)
            return Unauthorized(new { error = "Invalid token." });

        var settings = await _settingsService.GetSettingsAsync(userId);
        if (settings == null)
        {
            return NotFound(new { error = "Settings not found for this user." });
        }
        return Ok(settings);
    }

    [HttpPut("blocking-mode")]
    public async Task<IActionResult> UpdateBlockingMode([FromBody] UpdateBlockingModeRequestDTO model)
    {
        var userId = GetUserIdString();
        if (userId == null)
            return Unauthorized(new { error = "Invalid token." });

        if (model == null || string.IsNullOrWhiteSpace(model.Mode))
            return BadRequest(new { error = "Invalid blocking mode specified." });

        var result = await _settingsService.UpdateBlockingModeAsync(userId, model);

        if (result == null || !result.Success)
        {
            return BadRequest(new { error = "Invalid blocking mode specified." });
        }
        return Ok(new { message = "Blocking mode updated successfully." });
    }

    [HttpPut("working-hours")]
    public async Task<IActionResult> UpdateWorkingHours([FromBody] UpdateWorkingHoursRequestDTO model)
    {
        var userId = GetUserIdString();
        if (userId == null)
            return Unauthorized(new { error = "Invalid token." });

        if (model == null || string.IsNullOrWhiteSpace(model.Mode))
            return BadRequest(new { error = "Working hours mode is required." });
        if (model.Mode.ToLower() == "custom" && (string.IsNullOrWhiteSpace(model.StartTime) || string.IsNullOrWhiteSpace(model.EndTime)))
            return BadRequest(new { error = "Invalid time format or missing times for custom mode." });

        var result = await _settingsService.UpdateWorkingHoursAsync(userId, model);

        if (result == null || !result.Success)
        {
            return BadRequest(new { error = "Invalid time format or missing times for custom mode." });
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
        // Assuming the manual fix resulted in whitelistResult being the correct list or null
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

        var result = await _settingsService.AddToWhitelistAsync(userId, model);

        if (result == null) 
            return BadRequest(new { error = "Failed to add number to whitelist." });

        if (!result.Success)
        {
            // Default to Conflict based on spec for this endpoint's error case
            return Conflict(new { error = "Number already exists in the whitelist." });
        }

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

        var result = await _settingsService.RemoveFromWhitelistAsync(userId, number);

        if (result == null) 
            return BadRequest(new { error = "Failed to remove number from whitelist." });

        if (!result.Success)
        {
            // Default to NotFound based on spec for this endpoint's error case
            return NotFound(new { error = "Number not found in the whitelist." });
        }

        return Ok(new { message = "Number removed from whitelist successfully." });
    }
}