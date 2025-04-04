using CallCleaner.Application.Dtos.Reports; // Varsayılan
using CallCleaner.Application.Dtos.Core; // Varsayılan
using CallCleaner.Application.Services; // IReportService için eklendi
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Security.Claims; // UserId almak için eklendi
using System.Linq; // SelectMany için eklendi

namespace CallCleaner.Api.Controllers;

[Produces("application/json")]
[Consumes("application/json")]
[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    // Constructor enjeksiyonu
    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpPost]
    [Authorize] // Rapor göndermek yetkilendirme gerektirir
    // DTO ismi tahmin ediliyor: SubmitReportRequestDTO
    public async Task<IActionResult> SubmitReport([FromBody] SubmitReportRequestDTO model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        if (!ModelState.IsValid) 
            return BadRequest(new ApiResponseDTO<object> { Success = false, Message = "Invalid input", Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });

        var result = await _reportService.SubmitReportAsync(userId, model);
        if (result == null)
        {
            // Servis geçersiz spam türü için null döndürdü
            return BadRequest(new ApiResponseDTO<object> { Success = false, Message = "Invalid spam type specified." });
        }
        // Başarılı durumda 201 Created döndür
        return CreatedAtAction(null, result); // Yeni oluşturulan kaynağın URI'si belirtilebilir
    }

    [HttpGet("recent-calls")]
    [Authorize] // Kullanıcının kendi aramalarını görmesi yetkilendirme gerektirir
    // DTO ismi tahmin ediliyor: GetRecentCallsResponseDTO (List<RecentCallDTO> olabilir)
    public async Task<IActionResult> GetRecentCalls([FromQuery] int limit = 10)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var recentCalls = await _reportService.GetRecentCallsAsync(userId, limit);
        // Servis şimdilik boş liste döndürüyor
        return Ok(recentCalls); // List<RecentCallDTO> döndürür
    }

    [HttpGet("spam-types")]
    // Bu endpoint genel bilgi verdiği için yetkilendirme gerektirmeyebilir
    // DTO ismi tahmin ediliyor: GetSpamTypesResponseDTO (List<SpamTypeDTO> olabilir)
    public async Task<IActionResult> GetSpamTypes()
    {
        var spamTypes = await _reportService.GetSpamTypesAsync();
        return Ok(spamTypes); // List<SpamTypeDTO> döndürür
    }
} 