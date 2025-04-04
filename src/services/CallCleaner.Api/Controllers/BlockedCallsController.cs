using CallCleaner.Application.Services; // IBlockedCallsService için eklendi
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CallCleaner.Api.Controllers;

[Produces("application/json")]
[Consumes("application/json")]
[ApiController]
[Route("api/blocked-calls")]
[Authorize]
public class BlockedCallsController : ControllerBase
{
    private readonly IBlockedCallsService _blockedCallsService;
    public BlockedCallsController(IBlockedCallsService blockedCallsService)
    {
        _blockedCallsService = blockedCallsService;
    }

    private string GetUserIdString() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    [HttpGet]
    public async Task<IActionResult> GetBlockedCalls([FromQuery] int page = 1, [FromQuery] int limit = 20)
    {
        var userId = GetUserIdString();
        if (userId == null)
            return Unauthorized(new { error = "Invalid token." });

        if (page < 1) page = 1;
        if (limit < 1) limit = 1;
        var response = await _blockedCallsService.GetBlockedCallsAsync(userId, page, limit);

        if (response == null)
            return Ok(new { calls = new List<object>(), pagination = new { currentPage = page, totalPages = 0, totalCount = 0 } });

        return Ok(response);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var userId = GetUserIdString();
        if (userId == null)
            return Unauthorized(new { error = "Invalid token." });

        var stats = await _blockedCallsService.GetStatsAsync(userId);

        if (stats == null)
            return Ok(new { today = 0, thisWeek = 0, total = 0 });

        return Ok(stats);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBlockedCall(string id)
    {
        var userId = GetUserIdString();
        if (userId == null)
            return Unauthorized(new { error = "Invalid token." });

        if (string.IsNullOrWhiteSpace(id))
            return BadRequest(new { error = "Blocked call ID is required." });

        var result = await _blockedCallsService.DeleteBlockedCallAsync(userId, id);

        if (result == null)
            return BadRequest(new { error = "Failed to delete blocked call record." });

        if (!result.Success)
            return NotFound(new { error = "Blocked call record not found." });

        // Use spec success format
        return Ok(new { message = "Blocked call record deleted successfully." });
    }

    [HttpDelete] // DELETE /api/blocked-calls
    public async Task<IActionResult> DeleteAllBlockedCalls()
    {
        var userId = GetUserIdString();
        if (userId == null)
            return Unauthorized(new { error = "Invalid token." });

        var result = await _blockedCallsService.DeleteAllBlockedCallsAsync(userId);

        if (result == null || !result.Success)
            return BadRequest(new { error = "Failed to delete all blocked call records." });

        return Ok(new { message = "All blocked call records deleted successfully." });
    }

    [HttpPut("{id}/report-wrong")]
    public async Task<IActionResult> ReportWronglyBlocked(string id)
    {
        var userId = GetUserIdString();
        if (userId == null)
            return Unauthorized(new { error = "Invalid token." });

        if (string.IsNullOrWhiteSpace(id))
            return BadRequest(new { error = "Blocked call ID is required." });

        var result = await _blockedCallsService.ReportWronglyBlockedAsync(userId, id);

        if (result == null)
            return BadRequest(new { error = "Failed to report call." });

        if (!result.Success)
            return NotFound(new { error = "Blocked call record not found." });

        return Ok(new { message = "Call reported as incorrectly blocked." });
    }
}