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

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDTO model)
    {
        if (model == null || string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
            return BadRequest(new { error = "Email and password are required." });

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null || !(await _userManager.CheckPasswordAsync(user, model.Password)))
        {
            return Unauthorized(new { error = "Invalid email or password." });
        }

        var userFullName = user.FullName;

        var token = _tokenService.GenerateJwtToken(user);

        return Ok(new
        {
            userId = user.Id,
            token = token,
            name = userFullName
        });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDTO model)
    {
        if (model == null || string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password) || string.IsNullOrWhiteSpace(model.FullName))
            return BadRequest(new { error = "FullName, email, and password are required." });

        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            return BadRequest(new { error = "Email already registered." });
        }

        var appUser = new AppUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(appUser, model.Password);
        if (!createResult.Succeeded)
        {
            return BadRequest(new { error = "User registration failed.", details = createResult.Errors.Select(e => e.Description) });
        }

        return CreatedAtAction(nameof(Register), new
        {
            userId = appUser.Id,
            message = "User registered successfully."
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

        return Ok("Email confirmation endpoint - needs review based on requirements.");
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