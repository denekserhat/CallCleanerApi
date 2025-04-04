using CallCleaner.Application.Dtos.Reports; // Varsayılan
using CallCleaner.Application.Dtos.Core; // Varsayılan
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CallCleaner.Api.Controllers;

[Produces("application/json")]
[Consumes("application/json")]
[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    // TODO: Gerekli servisleri inject et (örneğin IReportService)
    // public ReportsController(IReportService reportService) { ... }

    [HttpPost]
    [Authorize] // Rapor göndermek yetkilendirme gerektirir
    // DTO ismi tahmin ediliyor: SubmitReportRequestDTO
    public async Task<IActionResult> SubmitReport([FromBody] SubmitReportRequestDTO model)
    {
        // TODO: Spam raporu gönderme mantığı
        if (!ModelState.IsValid) return BadRequest(new ApiResponseDTO<object> { Success = false, Message = "Invalid input", Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });
        await Task.CompletedTask;
        // Örnek Yanıt DTO: SubmitReportResponseDTO
        return StatusCode(201, new { reportId = "report_xyz789", message = "Report submitted successfully." }); // Geçici yanıt
        // Başarılı yanıt örneği: return Created("", responseDto);
        // Hata yanıtı örneği: return BadRequest(new ApiResponseDTO<object> { Success = false, Message = "Missing required fields" });
    }

    [HttpGet("recent-calls")]
    [Authorize] // Kullanıcının kendi aramalarını görmesi yetkilendirme gerektirir
    // DTO ismi tahmin ediliyor: GetRecentCallsResponseDTO (List<RecentCallDTO> olabilir)
    public async Task<IActionResult> GetRecentCalls([FromQuery] int limit = 10)
    {
        // TODO: Son aramaları getirme mantığı (limit kullanılacak)
        await Task.CompletedTask;
        // Örnek Yanıt DTO: GetRecentCallsResponseDTO veya List<RecentCallDTO>
        return Ok(new { Message = "Endpoint not implemented yet.", Limit = limit }); // Geçici yanıt
        // Başarılı yanıt örneği: return Ok(recentCallsDto);
    }

    [HttpGet("spam-types")]
    // Bu endpoint genel bilgi verdiği için yetkilendirme gerektirmeyebilir
    // DTO ismi tahmin ediliyor: GetSpamTypesResponseDTO (List<SpamTypeDTO> olabilir)
    public async Task<IActionResult> GetSpamTypes()
    {
        // TODO: Spam türlerini getirme mantığı (sabit liste olabilir)
        await Task.CompletedTask;
        // Örnek Yanıt DTO: GetSpamTypesResponseDTO veya List<SpamTypeDTO>
        return Ok(new { Message = "Endpoint not implemented yet." }); // Geçici yanıt
        // Başarılı yanıt örneği: return Ok(spamTypesDto);
    }
} 