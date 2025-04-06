using CallCleaner.Application.Dtos.Reports;
using CallCleaner.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CallCleaner.Api.Controllers;

[Produces("application/json")]
[Consumes("application/json")]
[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    private string GetUserIdString() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> SubmitReport([FromBody] SubmitReportRequestDTO model)
    {
        var userId = GetUserIdString();
        if (userId == null)
            return Unauthorized(new { error = "Invalid token." });

        if (model == null || string.IsNullOrWhiteSpace(model.PhoneNumber) || string.IsNullOrWhiteSpace(model.SpamType))
            return BadRequest(new { error = "Missing required fields (phoneNumber, spamType)." });

        var result = await _reportService.SubmitReportAsync(userId, model);

        if (result == null)
            return BadRequest(new { error = "Failed to submit report." });

        return StatusCode(StatusCodes.Status201Created, new { reportId = result.ReportId, message = "Report submitted successfully." });
    }

    [HttpGet("recent-calls")]
    [Authorize]
    public async Task<IActionResult> GetRecentCalls([FromQuery] int limit = 10)
    {
        var userId = GetUserIdString();
        if (userId == null)
            return Unauthorized(new { error = "Invalid token." });

        if (limit < 1) limit = 1;
        var recentCalls = await _reportService.GetRecentCallsAsync(userId, limit);

        return Ok(recentCalls ?? new List<RecentCallDTO>());
    }

    [HttpGet("spam-types")]
    public async Task<IActionResult> GetSpamTypes()
    {
        var spamTypes = _reportService.GetSpamTypes();
        return Ok(spamTypes ?? new List<SpamTypeDTO>());
    }
}