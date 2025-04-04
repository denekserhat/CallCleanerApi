using CallCleaner.Application.Dtos.Reports; // Tahmini DTO namespace
using CallCleaner.DataAccess;
using CallCleaner.Entities.Concrete;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CallCleaner.Application.Services;

public interface IReportService
{
    // SubmitReport için userId eklendi
    Task<SubmitReportResponseDTO> SubmitReportAsync(string userId, SubmitReportRequestDTO model);
    // GetRecentCalls için userId ve limit eklendi
    Task<List<RecentCallDTO>> GetRecentCallsAsync(string userId, int limit);
    // GetSpamTypes için parametreye gerek yok (genel bilgi)
    Task<List<SpamTypeDTO>> GetSpamTypesAsync();
}


public class ReportService : IReportService
{
    private readonly DataContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly AutoMapper.IMapper _mapper;

    // Constructor enjeksiyonu
    public ReportService(DataContext context, UserManager<AppUser> userManager, AutoMapper.IMapper mapper)
    {
        _context = context;
        _userManager = userManager;
        _mapper = mapper;
    }

    public async Task<SubmitReportResponseDTO> SubmitReportAsync(string userId, SubmitReportRequestDTO model)
    {
        if (!int.TryParse(userId, out int userIdInt))
            throw new ArgumentException("Invalid User ID format.");

        if (!Enum.TryParse<SpamType>(model.SpamType, true, out var spamTypeEnum))
        {
            return null; // Veya BadRequest için uygun bir dönüş
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
            Message = "Report submitted successfully."
        };
    }

    public async Task<List<RecentCallDTO>> GetRecentCallsAsync(string userId, int limit)
    {
        await Task.CompletedTask;
        Console.WriteLine($"Skipping GetRecentCallsAsync for user: {userId} on server-side.");
        return new List<RecentCallDTO>();
    }

    public async Task<List<SpamTypeDTO>> GetSpamTypesAsync()
    {
        var spamTypes = Enum.GetValues(typeof(SpamType))
                            .Cast<SpamType>()
                            .Select(e => new SpamTypeDTO { Id = e.ToString().ToLowerInvariant(), Label = e.ToString() })
                            .ToList();
        await Task.CompletedTask;
        return spamTypes;
    }
}