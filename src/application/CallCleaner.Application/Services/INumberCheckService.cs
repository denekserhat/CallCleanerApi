using CallCleaner.Application.Dtos.SpamDetection;
using CallCleaner.DataAccess;
using CallCleaner.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace CallCleaner.Application.Services;

public interface INumberCheckService
{
    Task<CheckNumberResponseDTO> CheckNumberAsync(string userId, CheckNumberRequestDTO model);
    Task<IncomingCallResponseDTO> CheckIncomingCallAsync(string userId, IncomingCallRequestDTO model);
    Task<GetNumberInfoResponseDTO> GetNumberInfoAsync(string userId, string number);
}

public class NumberCheckService : INumberCheckService
{
    private readonly DataContext _context;
    private readonly ISettingsService _settingsService;
    private readonly AutoMapper.IMapper _mapper;

    public NumberCheckService(DataContext context, ISettingsService settingsService, AutoMapper.IMapper mapper)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<CheckNumberResponseDTO> CheckNumberAsync(string userId, CheckNumberRequestDTO model)
    {
        if (!int.TryParse(userId, out int userIdInt))
            throw new ArgumentException("Geçersiz Kullanıcı ID formatı.");

        var phoneNumber = model.PhoneNumber;

        var whitelist = await _settingsService.GetWhitelistAsync(userId);
        if (whitelist?.Any(item => item.PhoneNumber == phoneNumber) == true)
        {
            Console.WriteLine($"Numara beyaz listede: {phoneNumber}, Kullanıcı: {userIdInt}");
            return new CheckNumberResponseDTO
            {
                IsSpam = false,
                SpamType = null,
                RiskScore = 0
            };
        }

        Console.WriteLine($"Yerel veritabanı kontrol ediliyor: {phoneNumber}");
        var reportedNumber = await _context.ReportedNumbers
                                        .AsNoTracking()
                                        .FirstOrDefaultAsync(n => n.PhoneNumber == phoneNumber);

        if (reportedNumber != null)
        {
            Console.WriteLine($"Numara yerel veritabanında bulundu. Rapor Sayısı: {reportedNumber.ReportCount}");
            bool isSpam = reportedNumber.ReportCount > 10;
            int riskScore;
            if (isSpam)
            {
                riskScore = 85;
            }
            else if (reportedNumber.ReportCount > 5)
            {
                riskScore = 50;
            }
            else
            {
                riskScore = 10;
            }

            return new CheckNumberResponseDTO
            {
                IsSpam = isSpam,
                SpamType = reportedNumber.CommonSpamType.ToString(),
                RiskScore = riskScore
            };
        }

        Console.WriteLine($"Numara yerel veritabanında bulunamadı, varsayılan sonuç dönülüyor: {phoneNumber}");

        return new CheckNumberResponseDTO
        {
            IsSpam = false,
            SpamType = null,
            RiskScore = 0
        };
    }

    public async Task<IncomingCallResponseDTO> CheckIncomingCallAsync(string userId, IncomingCallRequestDTO model)
    {
        string action = "allow";
        string reason = null;
        string phoneNumber = model.PhoneNumber;
        DateTime callTimestamp = model.Timestamp;

        var checkResult = await CheckNumberAsync(userId, new CheckNumberRequestDTO { PhoneNumber = phoneNumber });

        if (checkResult == null)
        {
            Console.WriteLine($"CheckNumberAsync null döndü: {phoneNumber}. İzin veriliyor.");
            action = "allow";
            reason = "Numara kontrol edilemedi";
            return new IncomingCallResponseDTO { Action = action, Reason = reason };
        }

        if (checkResult.RiskScore == 0)
        {
            Console.WriteLine($"Numara beyaz listede veya bilinmiyor: {phoneNumber}. İzin veriliyor.");
            action = "allow";
            reason = checkResult.IsSpam == false && checkResult.SpamType == null ? "Beyaz listede" : "Bilinmiyor";
            return new IncomingCallResponseDTO { Action = action, Reason = reason };
        }

        var settings = await _settingsService.GetSettingsAsync(userId);
        if (settings == null)
        {
            Console.WriteLine($"Kullanıcı ayarları alınamadı: {userId}. İzin veriliyor.");
            action = "allow";
            reason = "Kullanıcı ayarları alınamadı";
            return new IncomingCallResponseDTO { Action = action, Reason = reason };
        }

        if (settings.WorkingHours != null && 
            string.Equals(settings.WorkingHours.Mode, "custom", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(settings.WorkingHours.StartTime) && 
            !string.IsNullOrWhiteSpace(settings.WorkingHours.EndTime))
        {
            if (TimeOnly.TryParseExact(settings.WorkingHours.StartTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startTime) &&
                TimeOnly.TryParseExact(settings.WorkingHours.EndTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var endTime))
            {
                TimeOnly currentTime = TimeOnly.FromDateTime(callTimestamp);
                if (currentTime < startTime || currentTime > endTime)
                {
                    Console.WriteLine($"Arama çalışma saatleri dışında: {phoneNumber}. Engelleniyor.");
                    action = "block";
                    reason = "Çalışma saatleri dışında";
                    return new IncomingCallResponseDTO { Action = action, Reason = reason };
                }
            }
            else
            {
                Console.WriteLine($"Çalışma saatleri ayrıştırılamadı: {settings.WorkingHours.StartTime} - {settings.WorkingHours.EndTime}. Kontrol atlanıyor.");
            }
        }

        string blockingMode = settings.BlockingMode?.ToLowerInvariant();

        if (blockingMode == "all" || blockingMode == "known")
        {
            if (checkResult.IsSpam)
            {
                action = "block";
                reason = $"{checkResult.SpamType ?? "Bilinmeyen Spam"} (Engelleme Modu: {settings.BlockingMode})";
            }
            else
            {
                action = "allow";
            }
        }
        else if (blockingMode == "custom")
        {
            if (checkResult.IsSpam)
            {
                action = "block";
                reason = $"{checkResult.SpamType ?? "Bilinmeyen Spam"} (Yüksek Risk)";
            }
            else if (checkResult.RiskScore == 50)
            {
                action = "warn";
                reason = $"{checkResult.SpamType ?? "Bilinmeyen Spam"} (Düşük Risk Uyarısı)";
            }
            else
            {
                action = "allow";
            }
        }
        else
        {
            action = "allow";
            reason = "Bilinmeyen engelleme modu";
        }
        
        Console.WriteLine($"Son Karar: {action}, Neden: {reason ?? "Yok"}, Numara: {phoneNumber}");
        return new IncomingCallResponseDTO { Action = action, Reason = reason };
    }

    public async Task<GetNumberInfoResponseDTO> GetNumberInfoAsync(string userId, string number)
    {
        if (string.IsNullOrWhiteSpace(number)) 
            throw new ArgumentException("Numara parametresi boş olamaz.", nameof(number));

        Console.WriteLine($"Numara bilgisi veritabanından alınıyor: {number}");

        var reportedNumber = await _context.ReportedNumbers
                                        .Include(n => n.Comments)
                                        .AsNoTracking()
                                        .FirstOrDefaultAsync(n => n.PhoneNumber == number);

        if (reportedNumber == null)
        {
            Console.WriteLine($"Numara bulunamadı: {number}");
            return null;
        }

        Console.WriteLine($"Numara bulundu: {number}. Bilgiler hazırlanıyor.");
        bool isSpam = reportedNumber.ReportCount > 10;
        string spamType = reportedNumber.CommonSpamType.ToString();

        var commentsDto = _mapper.Map<List<NumberCommentDTO>>(reportedNumber.Comments);

        var response = new GetNumberInfoResponseDTO
        {
            PhoneNumber = reportedNumber.PhoneNumber,
            IsSpam = isSpam,
            SpamType = isSpam ? spamType : null,
            ReportCount = reportedNumber.ReportCount,
            FirstReported = reportedNumber.FirstReportedDate,
            LastReported = reportedNumber.LastReportedDate,
            Comments = commentsDto ?? new List<NumberCommentDTO>()
        };

        return response;
    }
}