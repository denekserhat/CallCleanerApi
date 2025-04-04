using CallCleaner.Application.Dtos.Reports;
using CallCleaner.Application.Dtos.Core;
using CallCleaner.DataAccess;
using CallCleaner.Entities.Concrete;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace CallCleaner.Application.Services;

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
        // TODO: Gelen raporu veritabanına kaydet
        await Task.Delay(10);
        Console.WriteLine($"Submitting report from user: {userId} for number: {model.PhoneNumber}, Type: {model.SpamType}");
        // Geçici DTO döndür
        return new SubmitReportResponseDTO
        {
            ReportId = "report_" + Guid.NewGuid().ToString().Substring(0, 8),
            Message = "Report submitted successfully."
        };
    }

    public async Task<List<RecentCallDTO>> GetRecentCallsAsync(string userId, int limit)
    {
        // TODO: Kullanıcının son arama kayıtlarını getir (muhtemelen cihazdan gelen log veya ayrı bir servis)
        await Task.Delay(10);
        Console.WriteLine($"Getting recent calls for user: {userId}, Limit: {limit}");
        // Geçici Liste döndür
        return new List<RecentCallDTO>
        {
            new RecentCallDTO { PhoneNumber = "02121112233", Timestamp = DateTime.UtcNow.AddMinutes(-15) },
            new RecentCallDTO { PhoneNumber = "05558887766", Timestamp = DateTime.UtcNow.AddMinutes(-45) }
        };
    }

    public async Task<List<SpamTypeDTO>> GetSpamTypesAsync()
    {
        // TODO: Tanımlı spam türlerini getir (sabit liste veya veritabanı)
        await Task.CompletedTask;
        Console.WriteLine("Getting spam types");
        // Geçici Liste döndür
        return new List<SpamTypeDTO>
        {
            new SpamTypeDTO { Id = "telemarketing", Label = "Telepazarlama" },
            new SpamTypeDTO { Id = "scam", Label = "Dolandırıcılık" },
            new SpamTypeDTO { Id = "annoying", Label = "Rahatsız Edici" },
            new SpamTypeDTO { Id = "other", Label = "Diğer" }
        };
    }
} 