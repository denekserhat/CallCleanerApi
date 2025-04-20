using CallCleaner.Application.Dtos.Auth;
using CallCleaner.Application.Dtos.Core;
using CallCleaner.Application.Dtos.Login;
using CallCleaner.Application.Services;
using CallCleaner.Entities.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;

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
    private readonly ILogger<AuthController> _logger;

    public AuthController(
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    IConfiguration configuration,
    IEmailService emailService,
    ITokenService tokenService,
    IMemoryCache cache,
    ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _emailService = emailService;
        _tokenService = tokenService;
        _cache = cache;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDTO model)
    {
        if (model == null || string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
            return BadRequest(new { error = "E-posta ve şifre gereklidir." });

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null || !(await _userManager.CheckPasswordAsync(user, model.Password)))
        {
            return Unauthorized(new { error = "Geçersiz e-posta veya şifre." });
        }

        // Generate JWT
        var accessToken = _tokenService.GenerateJwtToken(user);
        // Generate and store Refresh Token
        var refreshToken = await _tokenService.GenerateAndStoreRefreshTokenAsync(user.Id);

        // Return both tokens using the new DTO
        return Ok(new TokenResponseDTO
        {
            UserId = user.Id,
            FullName = user.FullName, // Or Name depending on your AppUser model
            AccessToken = accessToken,
            RefreshToken = refreshToken.RefreshToken // Return the token string
        });
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDTO model)
    {
        if (model == null || string.IsNullOrWhiteSpace(model.RefreshToken))
            return BadRequest(new { error = "Yenileme tokenı gereklidir." });

        (string? newAccessToken, UserRefreshToken? newRefreshToken) =
            await _tokenService.ValidateAndUseRefreshTokenAsync(model.RefreshToken);

        if (newAccessToken == null || newRefreshToken == null)
        {
            return Unauthorized(new { error = "Geçersiz veya süresi dolmuş yenileme tokenı." });
        }

        var user = await _userManager.FindByIdAsync(newRefreshToken.UserId.ToString());
        var fullName = user?.FullName;

        return Ok(new TokenResponseDTO
        {
            UserId = newRefreshToken.UserId,
            FullName = fullName,
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken.RefreshToken
        });
    }

    [HttpPost("logout")]
    [Authorize] // Optional: Only logged-in users can logout
    public async Task<IActionResult> Logout([FromBody] RevokeTokenRequestDTO model)
    {
        if (model == null || string.IsNullOrWhiteSpace(model.RefreshToken))
            return BadRequest(new { error = "Yenileme tokenı gereklidir." });

        var revoked = await _tokenService.RevokeRefreshTokenAsync(model.RefreshToken);

        if (!revoked)
        {
            // Maybe return NotFound or BadRequest if token doesn't exist or already revoked
            return BadRequest(new { error = "Yenileme tokenı iptal edilemedi veya bulunamadı." });
        }

        return Ok(new { message = "Başarıyla çıkış yapıldı." });
    }

    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDTO model)
    {
        _logger.LogInformation("E-posta {Email} için kayıt denemesi başlatıldı", model?.Email);

        if (model == null || !ModelState.IsValid)
        {
            _logger.LogWarning("Geçersiz model durumu nedeniyle kayıt denemesi başarısız oldu: {ModelState}",
                             JsonSerializer.Serialize(ModelState.Values.SelectMany(v => v.Errors)));
            return BadRequest(ModelState);
        }

        _logger.LogDebug("E-posta {Email} için mevcut kullanıcı kontrol ediliyor", model.Email);
        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            _logger.LogWarning("Kayıt denemesi başarısız: E-posta {Email} zaten kayıtlı.", model.Email);
            return BadRequest(new { error = "Bu e-posta adresi zaten kayıtlı." });
        }

        var newUser = new AppUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            EmailConfirmed = false // E-posta onayı gerektirdiği için false olarak başlat
        };

        _logger.LogInformation("E-posta {Email} ile yeni kullanıcı oluşturulmaya çalışılıyor", newUser.Email);
        var result = await _userManager.CreateAsync(newUser, model.Password);

        if (!result.Succeeded)
        {
            _logger.LogError("E-posta {Email} için kullanıcı oluşturma başarısız oldu. Hatalar: {IdentityErrors}",
                             newUser.Email,
                             JsonSerializer.Serialize(result.Errors));

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return BadRequest(ModelState);
        }

        _logger.LogInformation("E-posta {Email} için {UserId} ID'li kullanıcı başarıyla oluşturuldu. Onay e-postası gönderilecek.", newUser.Id, newUser.Email);

        // Onay e-postasını gönder
        try
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(newUser);
            // URL oluştururken scheme (http/https) ve host'u request'ten almak önemlidir.
            var callbackUrl = Url.Action(nameof(ConfirmEmail), "Auth", new { userId = newUser.Id, token = token }, Request.Scheme, Request.Host.ToString());

            if (string.IsNullOrEmpty(callbackUrl))
            {
                _logger.LogError("E-posta onay URL'i oluşturulamadı. Kullanıcı ID: {UserId}", newUser.Id);
                // Kullanıcı oluşturuldu ama e-posta gönderilemedi. Bu durumu nasıl ele alacağınıza karar verin.
                // Belki bir iç hata döndürebilir veya işlemi başarılı kabul edip daha sonra manuel gönderme şansı verebilirsiniz.
                // Şimdilik başarılı kabul edelim, ancak loglamak önemli.
            }
            else
            {
                await _emailService.SendMailAsync(newUser.Email,
                                               "Hesabınızı Onaylayın",
                                               $"Lütfen buraya tıklayarak hesabınızı onaylayın: <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>Onay Linki</a>");
                _logger.LogInformation("Onay e-postası başarıyla gönderildi: {Email}", newUser.Email);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kullanıcı {UserId} için onay e-postası gönderilirken hata oluştu.", newUser.Id);
            // E-posta gönderimi başarısız olsa bile kullanıcı oluşturuldu.
            // Bu hatayı nasıl yöneteceğinize karar verin (örn. kullanıcıya bilgi ver, arka planda tekrar dene vs.)
        }

        // Return 201 Created
        return CreatedAtAction(nameof(Register), new { userId = newUser.Id }, new { message = "Kullanıcı başarıyla kaydedildi. Lütfen e-postanızı kontrol ederek hesabınızı onaylayın." });
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
        _logger.LogInformation("E-posta onayı denemesi başlatıldı. Kullanıcı ID: {UserId}, Token: {Token}", userId, token);

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("E-posta onayı başarısız: Kullanıcı ID veya token eksik.");
            return BadRequest(new ApiResponseDTO<object> { Success = false, Message = "Gerekli bilgiler eksik." });
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("E-posta onayı başarısız: Kullanıcı bulunamadı. ID: {UserId}", userId);
            return NotFound(new ApiResponseDTO<object> { Success = false, Message = "Kullanıcı bulunamadı." });
        }

        // Token'ı doğrula ve kullanıcının EmailConfirmed alanını güncelle
        var result = await _userManager.ConfirmEmailAsync(user, token);
        if (result.Succeeded)
        {
            _logger.LogInformation("E-posta başarıyla onaylandı. Kullanıcı ID: {UserId}", userId);
            // Başarılı onay sonrası kullanıcıyı nereye yönlendireceğinize veya ne mesaj göstereceğinize karar verin.
            // Örneğin bir HTML sayfası veya basit bir mesaj döndürebilirsiniz.
            return Ok(new ApiResponseDTO<object> { Success = true, Message = "E-postanız başarıyla onaylandı." });
        }
        else
        {
            _logger.LogError("E-posta onayı başarısız oldu. Kullanıcı ID: {UserId}, Hatalar: {IdentityErrors}",
                             userId,
                             JsonSerializer.Serialize(result.Errors));
            // Hataları kullanıcıya göstermek yerine genel bir mesaj vermek daha iyi olabilir.
            return BadRequest(new ApiResponseDTO<object> { Success = false, Message = "E-posta onayı başarısız oldu. Lütfen tekrar deneyin veya destek ile iletişime geçin." });
        }
    }

    [HttpGet("verify-token")]
    [Authorize]
    public IActionResult VerifyToken()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
        {
            return Unauthorized(new { error = "Invalid or expired token." });
        }

        return Ok(new { userId = userId, isValid = true });
    }

    [HttpPut("update-profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequestDTO model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized(new { error = "Invalid token." });
        }

        if (model == null || string.IsNullOrWhiteSpace(model.Name))
        {
            return BadRequest(new { error = "Invalid profile data provided." });
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound(new { error = "User not found." });
        }

        bool changed = false;

        if (user.FullName != model.Name)
        {
            user.FullName = model.Name;
            changed = true;
        }

        if (!string.IsNullOrWhiteSpace(model.NewPassword))
        {
            var passwordChangeToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var passwordResult = await _userManager.ResetPasswordAsync(user, passwordChangeToken, model.NewPassword);
            if (!passwordResult.Succeeded)
            {
                return BadRequest(new { error = "Invalid profile data provided.", details = passwordResult.Errors.Select(e => e.Description) });
            }
            changed = true;
        }

        if (changed)
        {
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return BadRequest(new { error = "Profile update failed.", details = updateResult.Errors.Select(e => e.Description) });
            }
        }

        return Ok(new
        {
            userId = user.Id,
            name = user.FullName,
            message = "Profile updated successfully."
        });
    }
}