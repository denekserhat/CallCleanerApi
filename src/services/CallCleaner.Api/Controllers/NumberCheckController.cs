using CallCleaner.Application.Dtos.SpamDetection;
using CallCleaner.Application.Services; // Assuming ISpamDetectionService or similar
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CallCleaner.Api.Controllers;

[Produces("application/json")]
[Consumes("application/json")]
[ApiController]
public class NumberCheckController : ControllerBase
{
    private readonly INumberCheckService _numberCheckService;

    public NumberCheckController(INumberCheckService numberCheckService)
    {
        _numberCheckService = numberCheckService;
    }

    private string GetUserIdString() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    [HttpPost("api/check-number")]
    [Authorize]
    public async Task<IActionResult> CheckNumber([FromBody] CheckNumberRequestDTO model)
    {
        var userId = GetUserIdString(); // Although userId isn't in spec, service might need it
        if (userId == null)
            return Unauthorized(new { error = "Invalid token." });

        if (model == null || string.IsNullOrWhiteSpace(model.PhoneNumber))
            return BadRequest(new { error = "PhoneNumber is required." });

        // Assuming service returns object: { IsSpam, SpamType, RiskScore }
        var checkResult = await _numberCheckService.CheckNumberAsync(userId, model);

        if (checkResult == null)
        {
            // Return default non-spam result if service fails or returns null
            return Ok(new { isSpam = false, spamType = (string)null, riskScore = 0 });
        }

        // Map service result to spec format (adjust property names if needed)
        return Ok(new
        {
            isSpam = checkResult.IsSpam,
            spamType = checkResult.SpamType, // Assuming service returns string or null
            riskScore = checkResult.RiskScore
        });
    }

    [HttpPost("api/incoming-call")]
    [Authorize]
    public async Task<IActionResult> CheckIncomingCall([FromBody] IncomingCallRequestDTO model)
    {
        var userId = GetUserIdString();
        if (userId == null)
            return Unauthorized(new { error = "Invalid token." });

        if (model == null || string.IsNullOrWhiteSpace(model.PhoneNumber))
            return BadRequest(new { error = "PhoneNumber is required." });

        // Assuming service returns object: { Action, Reason } (e.g., "block", "allow", "warn")
        var incomingResult = await _numberCheckService.CheckIncomingCallAsync(userId, model);

        if (incomingResult == null)
        {
            // Default to allow if service fails
            return Ok(new { action = "allow", reason = (string)null });
        }

        // Map service result to spec format
        return Ok(new
        {
            action = incomingResult.Action,
            reason = incomingResult.Reason
        });
    }

    [HttpGet("api/number/{number}/info")]
    [Authorize]
    public async Task<IActionResult> GetNumberInfo(string number)
    {
        var userId = GetUserIdString(); // Service might need userId for context/history
        if (userId == null)
            return Unauthorized(new { error = "Invalid token." });

        if (string.IsNullOrWhiteSpace(number))
            return BadRequest(new { error = "Number parameter is required." });

        // Assuming service returns object matching spec or null if not found
        // Spec: { PhoneNumber, IsSpam, SpamType, ReportCount, FirstReported, LastReported, Comments: List<{ user, comment, timestamp }> }
        var numberInfo = await _numberCheckService.GetNumberInfoAsync(userId, number);

        if (numberInfo == null)
        {
            // Match spec error format
            return NotFound(new { error = "Information not found for this number." });
        }

        // Return the result directly assuming it matches the spec
        return Ok(numberInfo);
    }
}