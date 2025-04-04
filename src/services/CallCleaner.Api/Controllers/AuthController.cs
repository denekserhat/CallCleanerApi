using CallCleaner.Application.Dtos.Auth;
using CallCleaner.Application.Dtos.Core;
using CallCleaner.Application.Dtos.Login;
using CallCleaner.Application.Services;
using CallCleaner.Entities.Concrete;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using System.Text;

namespace CallCleaner.Api.Controllers;

[Produces("application/json")]
[Consumes("application/json")]
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly ITokenService _tokenService;
    private readonly IMemoryCache _cache;

    public AuthController(
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    IConfiguration configuration,
    IEmailService emailService,
    ITokenService tokenService,
    IMemoryCache cache)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _emailService = emailService;
        _tokenService = tokenService;
        _cache = cache;
    }

    //TODO: login olduktan sonra response'a refresh token eklenecek
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDTO model)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ApiResponseDTO<object>
            {
                Success = false,
                Message = "Validation failed",
                Errors = ModelState.Values.SelectMany(x => x.Errors.Select(e => e.ErrorMessage))
            });

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null || !(await _userManager.CheckPasswordAsync(user, model.Password)))
        {
            return Unauthorized(new ApiResponseDTO<object>
            {
                Success = false,
                Message = "Invalid login credentials"
            });
        }

        var token = _tokenService.GenerateJwtToken(user);

        return Ok(new ApiResponseDTO<LoginResponseDTO>
        {
            Success = true,
            Message = "Login successful",
            Data = new LoginResponseDTO
            {
                Token = token,
                Email = user.Email,
                UserId = user.Id
            }
        });
    }
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDTO model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiResponseDTO<object>
            {
                Success = false,
                Message = "Validation failed",
                Errors = ModelState.Values.SelectMany(x => x.Errors.Select(e => e.ErrorMessage))
            });
        }

        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            return Conflict(new ApiResponseDTO<object>
            {
                Success = false,
                Message = "Bu e-posta adresi zaten kayıtlı"
            });
        }

        var appUser = new AppUser
        {
            UserName = model.Email,
            Email = model.Email,
            EmailConfirmed = false
        };

        var createResult = await _userManager.CreateAsync(appUser, model.Password);
        if (!createResult.Succeeded)
        {
            return BadRequest(new ApiResponseDTO<object>
            {
                Success = false,
                Message = "Kayıt oluşturulurken hata oluştu",
                Errors = createResult.Errors.Select(e => e.Description)
            });
        }

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(appUser);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var confirmationLink = Url.Action(nameof(ConfirmEmail), "Auth", new { userId = appUser.Id, token = encodedToken }, Request.Scheme);

        var body = $"Hesabınızı onaylamak için lütfen <a href='{confirmationLink}'>buraya tıklayın</a>.";
        await _emailService.SendMailAsync(appUser.Email, "E-posta Doğrulama", body);

        return Ok(new ApiResponseDTO<RegisterResponseDTO>
        {
            Success = true,
            Message = "Kullanıcı başarıyla oluşturuldu! Lütfen e-postanıza gönderilen link ile hesabınızı onaylayın.",
            Data = new RegisterResponseDTO
            {
                UserId = appUser.Id,
                Email = appUser.Email
            }
        });
    }
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDTO model)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is null)
            return NotFound(new ApiResponseDTO<object>
            {
                Success = false,
                Message = "Bu e-posta adresi ile kayıtlı kullanıcı bulunamadı."
            });

        var resetCode = new Random().Next(100000, 999999).ToString();
        var cacheKey = $"password_reset:{user.Id}";
        _cache.Set(cacheKey, resetCode, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
        });

        var body = $"Şifrenizi sıfırlamak için kodunuz: {resetCode}";
        await _emailService.SendMailAsync(user.Email, "Şifre Sıfırlama Kodu", body);

        return Ok(new ApiResponseDTO<object>
        {
            Success = true,
            Message = "Şifre sıfırlama kodu e-posta adresinize gönderildi."
        });
    }
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO model)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is null)
            return NotFound(new ApiResponseDTO<object>
            {
                Success = false,
                Message = "Bu e-posta adresi ile kayıtlı kullanıcı bulunamadı."
            });

        var cacheKey = $"password_reset:{user.Id}";
        var storedCode = _cache.Get<string>(cacheKey);

        if (storedCode == null)
            return BadRequest(new ApiResponseDTO<object>
            {
                Success = false,
                Message = "Şifre sıfırlama kodu süresi dolmuş veya geçersiz."
            });

        if (storedCode != model.Code)
            return BadRequest(new ApiResponseDTO<object>
            {
                Success = false,
                Message = "Geçersiz şifre sıfırlama kodu."
            });

        var identityToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, identityToken, model.NewPassword);

        if (!result.Succeeded)
        {
            return BadRequest(new ApiResponseDTO<object>
            {
                Success = false,
                Message = "Şifre sıfırlanırken bir hata oluştu.",
                Errors = result.Errors.Select(e => e.Description)
            });
        }

        _cache.Remove(cacheKey);
        return Ok(new ApiResponseDTO<object>
        {
            Success = true,
            Message = "Şifreniz başarıyla sıfırlandı."
        });
    }
    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
            return BadRequest(new ApiResponseDTO<object>
            {
                Success = false,
                Message = "Kullanıcı ID veya token eksik."
            });

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(new ApiResponseDTO<object>
            {
                Success = false,
                Message = "Kullanıcı bulunamadı."
            });

        try
        {
            var decodedTokenBytes = WebEncoders.Base64UrlDecode(token);
            var decodedToken = Encoding.UTF8.GetString(decodedTokenBytes);

            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
            if (!result.Succeeded)
                return BadRequest(new ApiResponseDTO<object>
                {
                    Success = false,
                    Message = "E-posta doğrulaması başarısız oldu.",
                    Errors = result.Errors.Select(e => e.Description)
                });

            user.IsActive = true;
            await _userManager.UpdateAsync(user);

            return Ok(new ApiResponseDTO<object>
            {
                Success = true,
                Message = "E-posta başarıyla doğrulandı."
            });
        }
        catch (FormatException)
        {
            return BadRequest(new ApiResponseDTO<object>
            {
                Success = false,
                Message = "Geçersiz doğrulama linki."
            });
        }
    }
}