using CallCleaner.Application.Dtos.Reports; // Tahmini DTO namespace
using CallCleaner.DataAccess;
using CallCleaner.Entities.Concrete;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CallCleaner.Application.Services;

public interface IReportService
{
    Task<SubmitReportResponseDTO> SubmitReportAsync(string userId, SubmitReportRequestDTO model);
    Task<List<RecentCallDTO>> GetRecentCallsAsync(string userId, int limit);
    List<SpamTypeDTO> GetSpamTypes();
}


public class ReportService : IReportService
{
    private readonly DataContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly AutoMapper.IMapper _mapper;

    public ReportService(DataContext context, UserManager<AppUser> userManager, AutoMapper.IMapper mapper)
    {
        _context = context;
        _userManager = userManager;
        _mapper = mapper;
    }

    public async Task<SubmitReportResponseDTO> SubmitReportAsync(string userId, SubmitReportRequestDTO model)
    {
        if (!int.TryParse(userId, out int userIdInt))
            throw new ArgumentException("Geçersiz Kullanıcı ID formatı.");

        if (!Enum.TryParse<SpamType>(model.SpamType, true, out var spamTypeEnum))
        {
            return null; // Geçersiz spam türü
        }

        var reportedNumber = await _context
                                        .ReportedNumbers
                                        .FirstOrDefaultAsync(n => n.PhoneNumber == model.PhoneNumber);

        if (reportedNumber == null)
        {
            reportedNumber = new ReportedNumber(model.PhoneNumber);
            _context.ReportedNumbers.Add(reportedNumber);
            reportedNumber.FirstReportedDate = DateTime.UtcNow;
            reportedNumber.ReportCount = 1;
        }
        else
        {
            reportedNumber.ReportCount++;
            reportedNumber.LastReportedDate = DateTime.UtcNow;
        }

        var spamReport = new SpamReport
        {
            UserId = userIdInt,
            ReportedNumber = reportedNumber,
            PhoneNumberReported = model.PhoneNumber,
            ReportedSpamType = spamTypeEnum,
            Description = model.Description,
            CreatedDate = model.Timestamp ?? DateTime.UtcNow
        };

        _context.SpamReports.Add(spamReport);
        await _context.SaveChangesAsync();

        return new SubmitReportResponseDTO
        {
            ReportId = "report_" + spamReport.Id,
            Message = "Rapor başarıyla gönderildi."
        };
    }

    public async Task<List<RecentCallDTO>> GetRecentCallsAsync(string userId, int limit)
    {
        // IMPLEMENT ET: Kullanıcının son arama kayıtlarını getirme mantığını uygula.
        // Bu verinin kaynağı (istemci, başka bir servis?) netleştirilmelidir.
        // Şimdilik boş liste döndürülüyor.
        Console.WriteLine($"Sunucu tarafında GetRecentCallsAsync atlanıyor: {userId}");
        await Task.CompletedTask; // Async metot için geçici olarak eklendi, IMPLEMENT ET yorumuna uygun doldurulmalı.
        return new List<RecentCallDTO>();
    }

    public List<SpamTypeDTO> GetSpamTypes()
    {
        var spamTypes = Enum.GetValues(typeof(SpamType))
                            .Cast<SpamType>()
                            .Select(e => new SpamTypeDTO { Id = e.ToString().ToLowerInvariant(), Label = e.ToString() })
                            .ToList();
        return spamTypes;
    }
}