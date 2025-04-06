using Microsoft.Extensions.Configuration;

namespace CallCleaner.Application.Services;

public interface ISmsService
{
    Task SendSmsAsync(string phoneNumber, string message);
}

public class SmsService : ISmsService
{
    private readonly IConfiguration _configuration;

    public SmsService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendSmsAsync(string phoneNumber, string message)
    {
        // TODO: SMS entegrasyonu yapılacak
    }
}