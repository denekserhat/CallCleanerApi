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
        if (!int.TryParse(userId, out int userIdInt)) throw new ArgumentException("Invalid User ID format.");
        var userSettings = await _context.UserSettings.FirstOrDefaultAsync(s => s.UserId == userIdInt);
        if (userSettings == null) return null;
        return _mapper.Map<GetSettingsResponseDTO>(userSettings);
    }

    public async Task<ApiResponseDTO<object>> UpdateBlockingModeAsync(string userId, UpdateBlockingModeRequestDTO model)
    {
        if (!int.TryParse(userId, out int userIdInt)) return new ApiResponseDTO<object> { Success = false, Message = "Invalid User ID format." };
        var userSettings = await _context.UserSettings.FirstOrDefaultAsync(s => s.UserId == userIdInt);
        if (userSettings == null) return new ApiResponseDTO<object> { Success = false, Message = "Settings not found for this user." };
        if (!Enum.TryParse<BlockingMode>(model.Mode, true, out var newMode))
        {
            return new ApiResponseDTO<object> { Success = false, Message = "Invalid blocking mode specified." };
        }
        userSettings.BlockingMode = newMode;
        await _context.SaveChangesAsync();
        return new ApiResponseDTO<object> { Success = true, Message = "Blocking mode updated successfully." };
    }

    public async Task<ApiResponseDTO<object>> UpdateWorkingHoursAsync(string userId, UpdateWorkingHoursRequestDTO model)
    {
        if (!int.TryParse(userId, out int userIdInt)) return new ApiResponseDTO<object> { Success = false, Message = "Invalid User ID format." };
        var userSettings = await _context.UserSettings.FirstOrDefaultAsync(s => s.UserId == userIdInt);
        if (userSettings == null) return new ApiResponseDTO<object> { Success = false, Message = "Settings not found for this user." };
        if (!Enum.TryParse<WorkingHoursMode>(model.Mode, true, out var newMode))
        {
            return new ApiResponseDTO<object> { Success = false, Message = "Invalid working hours mode specified." };
        }
        TimeOnly? startTime = null;
        TimeOnly? endTime = null;
        if (newMode == WorkingHoursMode.Custom)
        {
            if (string.IsNullOrWhiteSpace(model.StartTime) || string.IsNullOrWhiteSpace(model.EndTime))
            {
                return new ApiResponseDTO<object> { Success = false, Message = "Start time and end time are required for custom mode." };
            }
            if (!TimeOnly.TryParseExact(model.StartTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedStartTime) ||
                !TimeOnly.TryParseExact(model.EndTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedEndTime))
            {
                return new ApiResponseDTO<object> { Success = false, Message = "Invalid time format. Please use HH:mm." };
            }
            startTime = parsedStartTime;
            endTime = parsedEndTime;
        }
        userSettings.WorkingHoursMode = newMode;
        userSettings.CustomStartTime = startTime;
        userSettings.CustomEndTime = endTime;
        await _context.SaveChangesAsync();
        return new ApiResponseDTO<object> { Success = true, Message = "Working hours updated successfully." };
    }

    public async Task<ApiResponseDTO<object>> UpdateNotificationsAsync(string userId, UpdateNotificationsRequestDTO model)
    {
        if (!int.TryParse(userId, out int userIdInt)) return new ApiResponseDTO<object> { Success = false, Message = "Invalid User ID format." };
        var userSettings = await _context.UserSettings.FirstOrDefaultAsync(s => s.UserId == userIdInt);
        if (userSettings == null) return new ApiResponseDTO<object> { Success = false, Message = "Settings not found for this user." };
        userSettings.NotificationsEnabled = model.Enabled;
        await _context.SaveChangesAsync();
        return new ApiResponseDTO<object> { Success = true, Message = "Notification settings updated successfully." };
    }

    public async Task<GetWhitelistResponseDTO> GetWhitelistAsync(string userId)
    {
        if (!int.TryParse(userId, out int userIdInt)) throw new ArgumentException("Invalid User ID format.");
        var whitelistEntries = await _context.WhitelistEntries
                                            .Where(w => w.UserId == userIdInt)
                                            .OrderByDescending(w => w.CreatedDate) // CreatedDate kullanıldı
                                            .ToListAsync();
        var responseDto = _mapper.Map<GetWhitelistResponseDTO>(whitelistEntries);
        return responseDto;
    }

    public async Task<ApiResponseDTO<object>> AddToWhitelistAsync(string userId, AddToWhitelistRequestDTO model)
    {
        if (!int.TryParse(userId, out int userIdInt)) return new ApiResponseDTO<object> { Success = false, Message = "Invalid User ID format." };
        bool exists = await _context.WhitelistEntries
                                .AnyAsync(w => w.UserId == userIdInt && w.PhoneNumber == model.Number);
        if (exists)
        {
            // IsConflict flag'i kaldırıldı
            return new ApiResponseDTO<object> { Success = false, Message = "Number already exists in the whitelist." };
        }
        var newEntry = _mapper.Map<WhitelistEntry>(model);
        newEntry.UserId = userIdInt; // Set accessor artık public
                                     // newEntry.CreatedDate BaseEntity tarafından otomatik ayarlanmalı (veya elle ayarlanabilir)
        newEntry.CreatedDate = DateTime.UtcNow;

        _context.WhitelistEntries.Add(newEntry);
        await _context.SaveChangesAsync();
        // IsCreated flag'i kaldırıldı
        return new ApiResponseDTO<object> { Success = true, Message = "Number added to whitelist successfully." };
    }

    public async Task<ApiResponseDTO<object>> RemoveFromWhitelistAsync(string userId, string number)
    {
        if (!int.TryParse(userId, out int userIdInt)) return new ApiResponseDTO<object> { Success = false, Message = "Invalid User ID format." };
        var entryToRemove = await _context.WhitelistEntries
                                        .FirstOrDefaultAsync(w => w.UserId == userIdInt && w.PhoneNumber == number);
        if (entryToRemove == null)
        {
            // IsNotFound flag'i kaldırıldı
            return new ApiResponseDTO<object> { Success = false, Message = "Number not found in the whitelist." };
        }
        _context.WhitelistEntries.Remove(entryToRemove);
        await _context.SaveChangesAsync();
        return new ApiResponseDTO<object> { Success = true, Message = "Number removed from whitelist successfully." };
    }
}