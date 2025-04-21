using CallCleaner.Application.Dtos.Auth;
using CallCleaner.Application.Dtos.Core;
using CallCleaner.Application.Dtos.Login;
using CallCleaner.Application.Services;
using CallCleaner.Entities.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
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
    [ProducesResponseType(typeof(ApiResponseDTO<TokenResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDTO model)
    {
        if (model == null || !ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
            return BadRequest(new ApiResponseDTO<object> { Success = false, Message = "Eksik veya geçersiz bilgi sağlandı.", Errors = errors });
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null || !(await _userManager.CheckPasswordAsync(user, model.Password)))
        {
            _logger.LogWarning("E-posta {Email} için giriş başarısız: Geçersiz kimlik bilgileri.", model.Email);
            return Unauthorized(new ApiResponseDTO<object> { Success = false, Message = "Geçersiz e-posta veya şifre." });
        }

        try
        {
            var accessToken = _tokenService.GenerateJwtToken(user);
            var refreshToken = await _tokenService.GenerateAndStoreRefreshTokenAsync(user.Id);

            _logger.LogInformation("Kullanıcı {UserId} başarıyla giriş yaptı.", user.Id);
            var responseData = new TokenResponseDTO
            {
                UserId = user.Id,
                FullName = user.FullName,
                AccessToken = accessToken,
                RefreshToken = refreshToken.RefreshToken
            };
            return Ok(new ApiResponseDTO<TokenResponseDTO> { Success = true, Data = responseData, Message = "Giriş başarılı." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kullanıcı {UserId} için token oluşturma/saklama sırasında hata oluştu", user.Id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponseDTO<object> { Success = false, Message = "Giriş işlemi sırasında sunucuda bir hata oluştu." });
        }
    }

    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(ApiResponseDTO<TokenResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDTO model)
    {
        if (model == null || string.IsNullOrWhiteSpace(model.RefreshToken))
            return BadRequest(new ApiResponseDTO<object> { Success = false, Message = "Yenileme tokenı gereklidir." });

        try
        {
            (string? newAccessToken, UserRefreshToken? newRefreshToken) =
                await _tokenService.ValidateAndUseRefreshTokenAsync(model.RefreshToken);

            if (newAccessToken == null || newRefreshToken == null)
            {
                _logger.LogWarning("Geçersiz veya süresi dolmuş yenileme tokenı ile token yenileme denemesi başarısız oldu.");
                return Unauthorized(new ApiResponseDTO<object> { Success = false, Message = "Oturumunuz zaman aşımına uğradı veya geçersiz. Lütfen tekrar giriş yapın." });
            }

            var user = await _userManager.FindByIdAsync(newRefreshToken.UserId.ToString());
            _logger.LogInformation("Kullanıcı {UserId} için token başarıyla yenilendi.", newRefreshToken.UserId);
            var responseData = new TokenResponseDTO
            {
                UserId = newRefreshToken.UserId,
                FullName = user?.FullName,
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken.RefreshToken
            };
            return Ok(new ApiResponseDTO<TokenResponseDTO> { Success = true, Data = responseData, Message = "Token başarıyla yenilendi." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token yenileme sırasında beklenmedik bir hata oluştu.");
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponseDTO<object> { Success = false, Message = "Token yenileme sırasında sunucuda bir hata oluştu." });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Logout([FromBody] RevokeTokenRequestDTO model)
    {
        if (model == null || string.IsNullOrWhiteSpace(model.RefreshToken))
        {
            return BadRequest(new ApiResponseDTO<object> { Success = false, Message = "Oturumu sonlandırmak için yenileme tokenı gereklidir." });
        }

        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        try
        {
            var revoked = await _tokenService.RevokeRefreshTokenAsync(model.RefreshToken);

            if (!revoked)
            {
                _logger.LogWarning("Kullanıcı {UserId} için yenileme tokenı iptal edilemedi. Token geçersiz veya zaten iptal edilmiş olabilir.", userId ?? "Bilinmeyen");
                return BadRequest(new ApiResponseDTO<object> { Success = false, Message = "Oturum sonlandırılamadı. Geçersiz veya süresi dolmuş token." });
            }

            _logger.LogInformation("Kullanıcı {UserId} başarıyla çıkış yaptı (yenileme tokenı iptal edildi).", userId ?? "Bilinmeyen");
            return Ok(new ApiResponseDTO<object> { Success = true, Message = "Başarıyla çıkış yapıldı." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kullanıcı {UserId} için çıkış işlemi sırasında beklenmedik bir hata oluştu.", userId ?? "Bilinmeyen");
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponseDTO<object> { Success = false, Message = "Çıkış işlemi sırasında sunucuda bir hata oluştu." });
        }
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDTO model)
    {
        if (model == null || !ModelState.IsValid)
        {
            _logger.LogWarning("Geçersiz model durumu nedeniyle kayıt denemesi başarısız oldu: {ModelState}",
                             JsonSerializer.Serialize(ModelState.Values.SelectMany(v => v.Errors)));
            var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
            return BadRequest(new ApiResponseDTO<object> { Success = false, Message = "Gerekli bilgiler eksik veya geçersiz.", Errors = errors });
        }

        _logger.LogDebug("E-posta {Email} için mevcut kullanıcı kontrol ediliyor", model.Email);
        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            _logger.LogWarning("Kayıt denemesi başarısız: E-posta {Email} zaten kayıtlı.", model.Email);
            return BadRequest(new ApiResponseDTO<object> { Success = false, Message = "Bu e-posta adresi zaten kayıtlı." });
        }

        var newUser = new AppUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            EmailConfirmed = false
        };

        _logger.LogDebug("E-posta {Email} ile yeni kullanıcı oluşturulmaya çalışılıyor", newUser.Email);
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
            return BadRequest(new ApiResponseDTO<object> { Success = false, Message = "Kullanıcı kaydı oluşturulamadı.", Errors = result.Errors.Select(e => e.Description) });
        }

        _logger.LogInformation("E-posta {Email} için {UserId} ID'li kullanıcı başarıyla oluşturuldu.", newUser.Id, newUser.Email);

        try
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(newUser);
            var callbackUrl = Url.Action(nameof(ConfirmEmail), "Auth", new { userId = newUser.Id, token = token }, Request.Scheme, Request.Host.ToString());

            if (string.IsNullOrEmpty(callbackUrl))
            {
                _logger.LogError("E-posta onay URL'i oluşturulamadı. Kullanıcı ID: {UserId}", newUser.Id);
            }
            else
            {
                await _emailService.SendMailAsync(newUser.Email,
                                               "Hesabınızı Onaylayın",
                                               $"Lütfen buraya tıklayarak hesabınızı onaylayın: <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>Onay Linki</a>");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kullanıcı {UserId} için onay e-postası gönderilirken hata oluştu.", newUser.Id);
        }

        var responseData = new { userId = newUser.Id };
        return CreatedAtAction(nameof(Register),
                               new { userId = newUser.Id },
                               new ApiResponseDTO<object> { Success = true, Data = responseData, Message = "Kullanıcı başarıyla kaydedildi. Lütfen e-postanızı kontrol ederek hesabınızı onaylayın." });
    }

    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDTO model)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
            return BadRequest(new ApiResponseDTO<object> { Success = false, Message = "Geçersiz istek: E-posta adresi gereklidir veya formatı hatalı.", Errors = errors });
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            _logger.LogWarning("Var olmayan e-posta adresi için şifre sıfırlama talebi: {Email}", model.Email);
            return NotFound(new ApiResponseDTO<object> { Success = false, Message = "Bu e-posta adresi ile kayıtlı kullanıcı bulunamadı." });
        }

        try
        {
            var resetCode = _tokenService.GenerateRandomCode();
            var cacheKey = $"password_reset:{user.Id}";
            _cache.Set(cacheKey, resetCode, TimeSpan.FromMinutes(15));

            var body = $"Şifrenizi sıfırlamak için kodunuz: {resetCode}";
            await _emailService.SendMailAsync(user.Email, "Şifre Sıfırlama Kodu", body);

            _logger.LogInformation("Kullanıcı {UserId} için şifre sıfırlama kodu başarıyla gönderildi: {Email}", user.Id, user.Email);
            return Ok(new ApiResponseDTO<object> { Success = true, Message = "Şifre sıfırlama kodu e-posta adresinize gönderildi." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kullanıcı {UserId} için şifre sıfırlama e-postası gönderilemedi.", user.Id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponseDTO<object> { Success = false, Message = "Şifre sıfırlama e-postası gönderilirken sunucuda bir hata oluştu." });
        }
    }

    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO model)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
            return BadRequest(new ApiResponseDTO<object> { Success = false, Message = "Gerekli bilgiler eksik veya geçersiz.", Errors = errors });
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            _logger.LogWarning("Var olmayan e-posta için şifre sıfırlama denemesi: {Email}", model.Email);
            return NotFound(new ApiResponseDTO<object> { Success = false, Message = "Bu e-posta adresi ile kayıtlı kullanıcı bulunamadı." });
        }

        var cacheKey = $"password_reset:{user.Id}";
        if (!_cache.TryGetValue(cacheKey, out string? storedCode))
        {
            _logger.LogWarning("Kullanıcı {UserId} için şifre sıfırlama başarısız: Kodun süresi dolmuş veya bulunamadı.", user.Id);
            return BadRequest(new ApiResponseDTO<object> { Success = false, Message = "Şifre sıfırlama kodu süresi dolmuş veya geçersiz." });
        }

        if (storedCode != model.Code)
        {
            _logger.LogWarning("Kullanıcı {UserId} için şifre sıfırlama başarısız: Sağlanan kod geçersiz.", user.Id);
            return BadRequest(new ApiResponseDTO<object> { Success = false, Message = "Geçersiz şifre sıfırlama kodu." });
        }

        try
        {
            var identityToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, identityToken, model.NewPassword);

            if (!result.Succeeded)
            {
                _logger.LogError("Kullanıcı {UserId} için şifre sıfırlama başarısız oldu. Hatalar: {IdentityErrors}",
                                 user.Id,
                                 JsonSerializer.Serialize(result.Errors));
                return BadRequest(new ApiResponseDTO<object> { Success = false, Message = "Şifre sıfırlanırken bir hata oluştu.", Errors = result.Errors.Select(e => e.Description) });
            }

            _cache.Remove(cacheKey);
            _logger.LogInformation("Kullanıcı {UserId} için şifre başarıyla sıfırlandı.", user.Id);
            return Ok(new ApiResponseDTO<object> { Success = true, Message = "Şifreniz başarıyla sıfırlandı." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kullanıcı {UserId} için şifre sıfırlama sırasında beklenmedik bir hata oluştu.", user.Id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponseDTO<object> { Success = false, Message = "Şifre sıfırlama sırasında sunucuda beklenmedik bir hata oluştu." });
        }
    }

    [HttpGet("confirm-email")]
    [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
    {
        _logger.LogInformation("E-posta onayı denemesi başlatıldı. Kullanıcı ID: {UserId}", userId);

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("E-posta onayı başarısız: Kullanıcı ID veya token eksik.");
            return BadRequest(new ApiResponseDTO<object> { Success = false, Message = "Gerekli onay bilgileri eksik." });
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("E-posta onayı başarısız: Kullanıcı bulunamadı. ID: {UserId}", userId);
            return NotFound(new ApiResponseDTO<object> { Success = false, Message = "Kullanıcı bulunamadı." });
        }

        var result = await _userManager.ConfirmEmailAsync(user, token);
        if (result.Succeeded)
        {
            _logger.LogInformation("E-posta başarıyla onaylandı. Kullanıcı ID: {UserId}", userId);
            return Ok(new ApiResponseDTO<object> { Success = true, Message = "E-postanız başarıyla onaylandı." });
        }
        else
        {
            _logger.LogError("E-posta onayı başarısız oldu. Kullanıcı ID: {UserId}, Hatalar: {IdentityErrors}",
                             userId,
                             JsonSerializer.Serialize(result.Errors));
            return BadRequest(new ApiResponseDTO<object> { Success = false, Message = "E-posta onayı başarısız oldu. Lütfen tekrar deneyin veya destek ile iletişime geçin." });
        }
    }

    [HttpGet("verify-token")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult VerifyToken()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
        {
            _logger.LogError("Yetkilendirilmiş istekte kullanıcı ID'si bulunamadı.");
            return Unauthorized();
        }

        _logger.LogInformation("Token başarıyla doğrulandı: Kullanıcı {UserId}", userId);
        return Ok(new ApiResponseDTO<object> { Success = true, Data = new { userId = userId, isValid = true }, Message = "Token geçerli." });
    }

    [HttpPut("update-profile")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequestDTO model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            _logger.LogError("Yetkilendirilmiş profil güncelleme isteğinde kullanıcı ID'si bulunamadı.");
            return Unauthorized(new ApiResponseDTO<object> { Success = false, Message = "Geçersiz oturum bilgisi." });
        }

        if (model == null || !ModelState.IsValid)
        {
            _logger.LogWarning("Kullanıcı {UserId} için geçersiz model nedeniyle profil güncelleme başarısız: {ModelState}",
                             userId,
                             JsonSerializer.Serialize(ModelState.Values.SelectMany(v => v.Errors)));
            var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
            return BadRequest(new ApiResponseDTO<object> { Success = false, Message = "Sağlanan bilgiler eksik veya geçersiz.", Errors = errors });
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            _logger.LogError("Profil güncelleme başarısız: Yetkili olmasına rağmen {UserId} ID'li kullanıcı bulunamadı.", userId);
            return NotFound(new ApiResponseDTO<object> { Success = false, Message = "Kullanıcı bulunamadı." });
        }

        bool changed = false;
        IdentityResult? passwordResult = null;

        try
        {
            if (user.FullName != model.Name && !string.IsNullOrWhiteSpace(model.Name))
            {
                user.FullName = model.Name;
                changed = true;
            }

            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                var passwordChangeToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                passwordResult = await _userManager.ResetPasswordAsync(user, passwordChangeToken, model.NewPassword);
                if (!passwordResult.Succeeded)
                {
                    _logger.LogError("Kullanıcı {UserId} için profil güncelleme sırasında şifre değiştirme başarısız. Hatalar: {IdentityErrors}",
                                     userId,
                                     JsonSerializer.Serialize(passwordResult.Errors));
                    return BadRequest(new ApiResponseDTO<object> { Success = false, Message = "Şifre güncellenemedi.", Errors = passwordResult.Errors.Select(e => e.Description) });
                }
                changed = true;
            }

            if (changed)
            {
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    _logger.LogError("Kullanıcı {UserId} için profil güncelleme başarısız oldu. Hatalar: {IdentityErrors}",
                                     userId,
                                     JsonSerializer.Serialize(updateResult.Errors));
                    return BadRequest(new ApiResponseDTO<object> { Success = false, Message = "Profil güncelleme başarısız oldu.", Errors = updateResult.Errors.Select(e => e.Description) });
                }
                _logger.LogInformation("Kullanıcı {UserId} için profil başarıyla güncellendi.", userId);
            }
            else
            {
                _logger.LogInformation("Kullanıcı {UserId} için profil güncelleme isteği alındı, ancak değişiklik yapılmadı.", userId);
            }

            return Ok(new ApiResponseDTO<object> { Success = true, Data = new { userId = user.Id, name = user.FullName }, Message = "Profil başarıyla güncellendi." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kullanıcı {UserId} için profil güncelleme sırasında beklenmedik bir hata oluştu.", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponseDTO<object> { Success = false, Message = "Profil güncellenirken sunucuda bir hata oluştu." });
        }
    }
}