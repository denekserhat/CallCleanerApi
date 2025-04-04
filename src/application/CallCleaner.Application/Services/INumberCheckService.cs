using CallCleaner.Application.Dtos.SpamDetection;

namespace CallCleaner.Application.Services;

public interface INumberCheckService
{
    // userId eklendi (kullanıcının ayarlarına göre kontrol gerekebilir)
    Task<CheckNumberResponseDTO> CheckNumberAsync(string userId, CheckNumberRequestDTO model);
    Task<IncomingCallResponseDTO> CheckIncomingCallAsync(string userId, IncomingCallRequestDTO model);
    Task<GetNumberInfoResponseDTO> GetNumberInfoAsync(string userId, string number);
}


public class NumberCheckService : INumberCheckService
{
    // TODO: Gerekli bağımlılıkları inject et (örn. DbContext, IExternalSpamCheckService)
    // public NumberCheckService(...) { ... }

    public async Task<CheckNumberResponseDTO> CheckNumberAsync(string userId, CheckNumberRequestDTO model)
    {
        // TODO: Numarayı veritabanında/harici serviste kontrol et
        // TODO: Kullanıcı ayarlarını dikkate al (örn. beyaz liste)
        await Task.Delay(10);
        Console.WriteLine($"Checking number: {model.PhoneNumber} for user: {userId}");
        // Geçici DTO döndür
        bool isSpam = model.PhoneNumber.StartsWith("0850"); // Basit kural
        return new CheckNumberResponseDTO
        {
            IsSpam = isSpam,
            SpamType = isSpam ? "Telepazarlama" : null,
            RiskScore = isSpam ? 85 : 10
        };
    }

    public async Task<IncomingCallResponseDTO> CheckIncomingCallAsync(string userId, IncomingCallRequestDTO model)
    {
        // TODO: Gelen aramayı anlık kontrol et (CheckNumberAsync + Kullanıcı Ayarları)
        await Task.Delay(10);
        Console.WriteLine($"Checking incoming call: {model.PhoneNumber} at {model.Timestamp} for user: {userId}");
        // Geçici DTO döndür
        string action = "allow";
        string reason = null;
        if (model.PhoneNumber.StartsWith("0850"))
        {
            action = "block";
            reason = "Yüksek riskli spam (Telepazarlama)";
        }
        return new IncomingCallResponseDTO
        {
            Action = action,
            Reason = reason
        };
    }

    public async Task<GetNumberInfoResponseDTO> GetNumberInfoAsync(string userId, string number)
    {
        // TODO: Numara hakkında detaylı bilgi getir (raporlar, yorumlar vb.)
        await Task.Delay(10);
        Console.WriteLine($"Getting info for number: {number} (requested by user: {userId})");
        // TODO: Numara bulunamazsa hata yönetimi
        // Geçici DTO döndür
        bool isSpam = number.StartsWith("0850");
        return new GetNumberInfoResponseDTO
        {
            PhoneNumber = number,
            IsSpam = isSpam,
            SpamType = isSpam ? "Telepazarlama" : null,
            ReportCount = isSpam ? 15 : 0,
            FirstReported = isSpam ? DateTime.UtcNow.AddMonths(-6) : null,
            LastReported = isSpam ? DateTime.UtcNow.AddDays(-1) : null,
            Comments = isSpam ? new List<NumberCommentDTO> { new NumberCommentDTO { User = "user_abc", Comment = "Sürekli sigorta.", Timestamp = DateTime.UtcNow.AddDays(-5) } } : new List<NumberCommentDTO>()
        };
    }
}