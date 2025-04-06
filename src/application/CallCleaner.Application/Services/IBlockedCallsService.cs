using CallCleaner.Application.Dtos.BlockedCalls;
using CallCleaner.Application.Dtos.Core;
using CallCleaner.DataAccess;
using CallCleaner.Entities.Concrete;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CallCleaner.Application.Services;

public interface IBlockedCallsService
{
    Task<GetBlockedCallsResponseDTO> GetBlockedCallsAsync(string userId, int page, int limit);
    Task<GetBlockedCallsStatsResponseDTO> GetStatsAsync(string userId);
    Task<ApiResponseDTO<object>> DeleteBlockedCallAsync(string userId, string callId);
    Task<ApiResponseDTO<object>> DeleteAllBlockedCallsAsync(string userId);
    Task<ApiResponseDTO<object>> ReportWronglyBlockedAsync(string userId, string callId);
}

public class BlockedCallsService : IBlockedCallsService
{
    private readonly DataContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly AutoMapper.IMapper _mapper;

    public BlockedCallsService(DataContext context, UserManager<AppUser> userManager, AutoMapper.IMapper mapper)
    {
        _context = context;
        _userManager = userManager;
        _mapper = mapper;
    }

    public async Task<GetBlockedCallsResponseDTO> GetBlockedCallsAsync(string userId, int page, int limit)
    {
        if (!int.TryParse(userId, out int userIdInt)) throw new ArgumentException("Invalid User ID format.");

        var query = _context.BlockedCalls.Where(bc => bc.UserId == userIdInt);

        var totalCount = await query.CountAsync();

        var blockedCalls = await query.OrderByDescending(bc => bc.CreatedDate)
                                      .Skip((page - 1) * limit)
                                      .Take(limit)
                                      .ToListAsync();

        var mappedCalls = _mapper.Map<List<BlockedCallDTO>>(blockedCalls);

        return new GetBlockedCallsResponseDTO
        {
            Calls = mappedCalls,
            Pagination = new PaginationInfoDTO
            {
                CurrentPage = page,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)limit)
            }
        };
    }

    public async Task<GetBlockedCallsStatsResponseDTO> GetStatsAsync(string userId)
    {
        if (!int.TryParse(userId, out int userIdInt)) throw new ArgumentException("Invalid User ID format.");

        var todayStart = DateTime.UtcNow.Date;
        var weekStart = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek + 1);

        var userBlockedCalls = _context.BlockedCalls.Where(bc => bc.UserId == userIdInt);

        var todayCount = await userBlockedCalls.CountAsync(bc => bc.CreatedDate >= todayStart);
        var thisWeekCount = await userBlockedCalls.CountAsync(bc => bc.CreatedDate >= weekStart);
        var totalCount = await userBlockedCalls.CountAsync();

        return new GetBlockedCallsStatsResponseDTO
        {
            Today = todayCount,
            ThisWeek = thisWeekCount,
            Total = totalCount
        };
    }

    public async Task<ApiResponseDTO<object>> DeleteBlockedCallAsync(string userId, string callId)
    {
        if (!int.TryParse(userId, out int userIdInt)) return new ApiResponseDTO<object> { Success = false, Message = "Invalid User ID format." };
        if (!int.TryParse(callId, out int callIdInt)) return new ApiResponseDTO<object> { Success = false, Message = "Invalid Call ID format." };

        var callToDelete = await _context.BlockedCalls
                                      .FirstOrDefaultAsync(bc => bc.Id == callIdInt && bc.UserId == userIdInt);

        if (callToDelete == null)
        {
            return new ApiResponseDTO<object> { Success = false, Message = "Blocked call record not found." };
        }

        _context.BlockedCalls.Remove(callToDelete);
        await _context.SaveChangesAsync();

        return new ApiResponseDTO<object> { Success = true, Message = "Blocked call record deleted successfully." };
    }

    public async Task<ApiResponseDTO<object>> DeleteAllBlockedCallsAsync(string userId)
    {
        if (!int.TryParse(userId, out int userIdInt)) return new ApiResponseDTO<object> { Success = false, Message = "Invalid User ID format." };

        var callsToDelete = await _context.BlockedCalls
                                        .Where(bc => bc.UserId == userIdInt)
                                        .ToListAsync();

        if (!callsToDelete.Any())
        {
            // Silinecek kayıt yoksa başarılı kabul edilebilir
            return new ApiResponseDTO<object> { Success = true, Message = "No blocked call records found to delete." };
        }

        _context.BlockedCalls.RemoveRange(callsToDelete);
        await _context.SaveChangesAsync();

        return new ApiResponseDTO<object> { Success = true, Message = "All blocked call records deleted successfully." };
    }

    public async Task<ApiResponseDTO<object>> ReportWronglyBlockedAsync(string userId, string callId)
    {
        if (!int.TryParse(userId, out int userIdInt)) return new ApiResponseDTO<object> { Success = false, Message = "Invalid User ID format." };
        if (!int.TryParse(callId, out int callIdInt)) return new ApiResponseDTO<object> { Success = false, Message = "Invalid Call ID format." };

        var callToReport = await _context.BlockedCalls
                                        .FirstOrDefaultAsync(bc => bc.Id == callIdInt && bc.UserId == userIdInt);

        if (callToReport == null)
        {
            return new ApiResponseDTO<object> { Success = false, Message = "Blocked call record not found." };
        }

        // Set accessor düzeltildi
        callToReport.ReportedAsIncorrect = true;
        // IMPLEMENT ET: Yanlış engelleme raporlaması için ek işlemler gerekebilir (örn. loglama, ayrı tabloya kayıt).

        await _context.SaveChangesAsync();

        return new ApiResponseDTO<object> { Success = true, Message = "Call reported as incorrectly blocked." };
    }
}