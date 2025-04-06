using CallCleaner.Application.Dtos.Core;
using CallCleaner.Application.Dtos.Settings;
using CallCleaner.DataAccess; // DataContext için eklendi
using CallCleaner.Entities.Concrete; // AppUser ve diğer entity'ler için eklendi
using Microsoft.AspNetCore.Identity; // UserManager için eklendi
using Microsoft.EntityFrameworkCore; // EF Core metodları için eklendi
using System.Globalization;


namespace CallCleaner.Application.Services;

public interface ISettingsService
{
    // userId parametresi, ayarların hangi kullanıcıya ait olduğunu belirtmek için eklendi.
    Task<GetSettingsResponseDTO> GetSettingsAsync(string userId);
    Task<ApiResponseDTO<object>> UpdateBlockingModeAsync(string userId, UpdateBlockingModeRequestDTO model);
    Task<ApiResponseDTO<object>> UpdateWorkingHoursAsync(string userId, UpdateWorkingHoursRequestDTO model);
    Task<ApiResponseDTO<object>> UpdateNotificationsAsync(string userId, UpdateNotificationsRequestDTO model);
    Task<GetWhitelistResponseDTO> GetWhitelistAsync(string userId);
    Task<ApiResponseDTO<object>> AddToWhitelistAsync(string userId, AddToWhitelistRequestDTO model);
    Task<ApiResponseDTO<object>> RemoveFromWhitelistAsync(string userId, string number);
}


public class SettingsService : ISettingsService
{
    private readonly DataContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly AutoMapper.IMapper _mapper;


    // Constructor enjeksiyonu
    public SettingsService(DataContext context, UserManager<AppUser> userManager, AutoMapper.IMapper mapper)
    {
        _context = context;
        _userManager = userManager;
        _mapper = mapper;
    }

    public async Task<GetSettingsResponseDTO> GetSettingsAsync(string userId)
    {
        if (!int.TryParse(userId, out int userIdInt)) throw new ArgumentException("Geçersiz Kullanıcı ID formatı.");
        var userSettings = await _context.UserSettings.FirstOrDefaultAsync(s => s.UserId == userIdInt);
        if (userSettings == null) return null;
        return _mapper.Map<GetSettingsResponseDTO>(userSettings);
    }

    public async Task<ApiResponseDTO<object>> UpdateBlockingModeAsync(string userId, UpdateBlockingModeRequestDTO model)
    {
        if (!int.TryParse(userId, out int userIdInt)) return new ApiResponseDTO<object> { Success = false, Message = "Geçersiz Kullanıcı ID formatı." };
        var userSettings = await _context.UserSettings.FirstOrDefaultAsync(s => s.UserId == userIdInt);
        if (userSettings == null) return new ApiResponseDTO<object> { Success = false, Message = "Bu kullanıcı için ayar bulunamadı." };
        if (!Enum.TryParse<BlockingMode>(model.Mode, true, out var newMode))
        {
            return new ApiResponseDTO<object> { Success = false, Message = "Geçersiz engelleme modu belirtildi." };
        }
        userSettings.BlockingMode = newMode;
        await _context.SaveChangesAsync();
        return new ApiResponseDTO<object> { Success = true, Message = "Engelleme modu başarıyla güncellendi." };
    }

    public async Task<ApiResponseDTO<object>> UpdateWorkingHoursAsync(string userId, UpdateWorkingHoursRequestDTO model)
    {
        if (!int.TryParse(userId, out int userIdInt)) return new ApiResponseDTO<object> { Success = false, Message = "Geçersiz Kullanıcı ID formatı." };
        var userSettings = await _context.UserSettings.FirstOrDefaultAsync(s => s.UserId == userIdInt);
        if (userSettings == null) return new ApiResponseDTO<object> { Success = false, Message = "Bu kullanıcı için ayar bulunamadı." };
        if (!Enum.TryParse<WorkingHoursMode>(model.Mode, true, out var newMode))
        {
            return new ApiResponseDTO<object> { Success = false, Message = "Geçersiz çalışma saati modu belirtildi." };
        }
        TimeOnly? startTime = null;
        TimeOnly? endTime = null;
        if (newMode == WorkingHoursMode.Custom)
        {
            if (string.IsNullOrWhiteSpace(model.StartTime) || string.IsNullOrWhiteSpace(model.EndTime))
            {
                return new ApiResponseDTO<object> { Success = false, Message = "Özel mod için başlangıç ve bitiş saati gereklidir." };
            }
            if (!TimeOnly.TryParseExact(model.StartTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedStartTime) ||
                !TimeOnly.TryParseExact(model.EndTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedEndTime))
            {
                return new ApiResponseDTO<object> { Success = false, Message = "Geçersiz saat formatı. Lütfen HH:mm kullanın." };
            }
            startTime = parsedStartTime;
            endTime = parsedEndTime;
        }
        userSettings.WorkingHoursMode = newMode;
        userSettings.CustomStartTime = startTime;
        userSettings.CustomEndTime = endTime;
        await _context.SaveChangesAsync();
        return new ApiResponseDTO<object> { Success = true, Message = "Çalışma saatleri başarıyla güncellendi." };
    }

    public async Task<ApiResponseDTO<object>> UpdateNotificationsAsync(string userId, UpdateNotificationsRequestDTO model)
    {
        if (!int.TryParse(userId, out int userIdInt)) return new ApiResponseDTO<object> { Success = false, Message = "Geçersiz Kullanıcı ID formatı." };
        var userSettings = await _context.UserSettings.FirstOrDefaultAsync(s => s.UserId == userIdInt);
        if (userSettings == null) return new ApiResponseDTO<object> { Success = false, Message = "Bu kullanıcı için ayar bulunamadı." };
        userSettings.NotificationsEnabled = model.Enabled;
        await _context.SaveChangesAsync();
        return new ApiResponseDTO<object> { Success = true, Message = "Bildirim ayarları başarıyla güncellendi." };
    }

    public async Task<GetWhitelistResponseDTO> GetWhitelistAsync(string userId)
    {
        if (!int.TryParse(userId, out int userIdInt)) throw new ArgumentException("Geçersiz Kullanıcı ID formatı.");
        var whitelistEntries = await _context.WhitelistEntries
                                            .Where(w => w.UserId == userIdInt)
                                            .OrderByDescending(w => w.CreatedDate) // CreatedDate kullanıldı
                                            .ToListAsync();
        var responseDto = _mapper.Map<GetWhitelistResponseDTO>(whitelistEntries);
        return responseDto;
    }

    public async Task<ApiResponseDTO<object>> AddToWhitelistAsync(string userId, AddToWhitelistRequestDTO model)
    {
        if (!int.TryParse(userId, out int userIdInt)) return new ApiResponseDTO<object> { Success = false, Message = "Geçersiz Kullanıcı ID formatı." };
        bool exists = await _context.WhitelistEntries
                                .AnyAsync(w => w.UserId == userIdInt && w.PhoneNumber == model.Number);
        if (exists)
        {
            // IsConflict flag'i kaldırıldı
            return new ApiResponseDTO<object> { Success = false, Message = "Numara zaten beyaz listede mevcut." };
        }
        var newEntry = _mapper.Map<WhitelistEntry>(model);
        newEntry.UserId = userIdInt;
        newEntry.CreatedDate = DateTime.UtcNow;

        _context.WhitelistEntries.Add(newEntry);
        await _context.SaveChangesAsync();
        return new ApiResponseDTO<object> { Success = true, Message = "Numara başarıyla beyaz listeye eklendi." };
    }

    public async Task<ApiResponseDTO<object>> RemoveFromWhitelistAsync(string userId, string number)
    {
        if (!int.TryParse(userId, out int userIdInt)) return new ApiResponseDTO<object> { Success = false, Message = "Geçersiz Kullanıcı ID formatı." };
        var entryToRemove = await _context.WhitelistEntries
                                        .FirstOrDefaultAsync(w => w.UserId == userIdInt && w.PhoneNumber == number);
        if (entryToRemove == null)
        {
            return new ApiResponseDTO<object> { Success = false, Message = "Numara beyaz listede bulunamadı." };
        }
        _context.WhitelistEntries.Remove(entryToRemove);
        await _context.SaveChangesAsync();
        return new ApiResponseDTO<object> { Success = true, Message = "Numara başarıyla beyaz listeden kaldırıldı." };
    }
}