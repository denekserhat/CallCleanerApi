using CallCleaner.Application.Dtos.Core;
using CallCleaner.Application.Services; // IBlockedCallsService için eklendi
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Security.Claims; // UserId almak için eklendi

namespace CallCleaner.Api.Controllers;

[Produces("application/json")]
[Consumes("application/json")]
[ApiController]
[Route("api/blocked-calls")]
[Authorize]
public class BlockedCallsController : ControllerBase
{
    private readonly IBlockedCallsService _blockedCallsService;

    // Constructor enjeksiyonu
    public BlockedCallsController(IBlockedCallsService blockedCallsService)
    {
        _blockedCallsService = blockedCallsService;
    }

    [HttpGet]
    public async Task<IActionResult> GetBlockedCalls([FromQuery] int page = 1, [FromQuery] int limit = 20)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        // Sayfa ve limit validasyonu eklenebilir
        if (page < 1) page = 1;
        if (limit < 1) limit = 10;
        if (limit > 100) limit = 100; // Max limit

        var response = await _blockedCallsService.GetBlockedCallsAsync(userId, page, limit);
        return Ok(response); // GetBlockedCallsResponseDTO döndürür
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var stats = await _blockedCallsService.GetStatsAsync(userId);
        return Ok(stats); // GetBlockedCallsStatsResponseDTO döndürür
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBlockedCall(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(id)) return BadRequest("ID cannot be empty.");

        var result = await _blockedCallsService.DeleteBlockedCallAsync(userId, id);
        if (!result.Success)
        {
            // Mesaj "Blocked call record not found." ise NotFound dön
            if (result.Message.Contains("not found"))
            {
                return NotFound(result);
            }
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteAllBlockedCalls()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var result = await _blockedCallsService.DeleteAllBlockedCallsAsync(userId);
        // Bu işlem genellikle başarılı olur, ama yine de kontrol edilebilir
        return Ok(result);
    }

    [HttpPut("{id}/report-wrong")]
    public async Task<IActionResult> ReportWronglyBlocked(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(id)) return BadRequest("ID cannot be empty.");

        var result = await _blockedCallsService.ReportWronglyBlockedAsync(userId, id);
        if (!result.Success)
        {
            if (result.Message.Contains("not found"))
            {
                return NotFound(result);
            }
            return BadRequest(result);
        }
        return Ok(result);
    }
}