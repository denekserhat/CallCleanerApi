using CallCleaner.Application.Dtos.SpamDetection;
using CallCleaner.Application.Dtos.Core;
using System.Threading.Tasks;

namespace CallCleaner.Application.Services;

public interface INumberCheckService
{
    // userId eklendi (kullanıcının ayarlarına göre kontrol gerekebilir)
    Task<CheckNumberResponseDTO> CheckNumberAsync(string userId, CheckNumberRequestDTO model);
    Task<IncomingCallResponseDTO> CheckIncomingCallAsync(string userId, IncomingCallRequestDTO model);
    Task<GetNumberInfoResponseDTO> GetNumberInfoAsync(string userId, string number);
} 