using CallCleaner.Application.Dtos.Auth;
using CallCleaner.Application.Dtos.Core;
using CallCleaner.Application.Dtos.Login;
using CallCleaner.Application.Services;
using CallCleaner.Entities.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

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
    private readonly ISmsService _smsService;
    private readonly ITokenService _tokenService;
    private readonly IMemoryCache _cache;

    public AuthController(
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    IConfiguration configuration,
    IEmailService emailService,
    ISmsService smsService,
    ITokenService tokenService,
    IMemoryCache cache)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _emailService = emailService;
        _smsService = smsService;
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

        var emailCode = new Random().Next(100000, 999999).ToString();
        var cacheKey = $"email_verification:{appUser.Id}";
        _cache.Set(cacheKey, emailCode, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        });

        var body = $"Email adresinizi doğrulamak için kod: {emailCode}";
        await _emailService.SendMailAsync(appUser.Email, "E-posta Doğrulama Kodu", body);

        return Ok(new ApiResponseDTO<RegisterResponseDTO>
        {
            Success = true,
            Message = "Kullanıcı başarılı! Lütfen e-postanıza gönderilen kodu onaylayın",
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
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid Request",
                detail: "Invalid email address");

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        // Send email logic (not implemented)

        return Ok(new ProblemDetails
        {
            Status = StatusCodes.Status200OK,
            Title = "Success",
            Detail = "Password reset link has been sent to your email"
        });
    }
    [Authorize]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO model)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is null)
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid Request",
                detail: "Invalid email address");

        var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return ValidationProblem(ModelState);
        }

        return Ok(new ProblemDetails
        {
            Status = StatusCodes.Status200OK,
            Title = "Success",
            Detail = "Password reset successfully"
        });
    }
    [HttpPost("send-confirmation")]
    public async Task<IActionResult> SendConfirmation([FromBody] SendConfirmationDTO model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
            return NotFound(new ApiResponseDTO<object>
            {
                Success = false,
                Message = "Kullanıcı bulunamadı"
            });

        // Email doğrulama
        if (!string.IsNullOrEmpty(model.Email))
        {
            var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var emailConfirmationLink = Url.Action(nameof(ConfirmEmail), "Authentication",
                new { email = model.Email, token = emailToken }, Request.Scheme);

            await _emailService.SendMailAsync(model.Email, "Email Doğrulama",
                $"Email adresinizi doğrulamak için <a href='{emailConfirmationLink}'>tıklayınız</a>");
        }

        return Ok(new ApiResponseDTO<object>
        {
            Success = true,
            Message = "Doğrulama kodları gönderildi"
        });
    }
    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailDTO model)
    {


        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
            return NotFound(new ApiResponseDTO<object>
            {
                Success = false,
                Message = "Kullanıcı bulunamadı"
            });

        var cacheKey = $"email_verification:{user.Id}";
        var storedCode = _cache.Get(cacheKey);
        if (storedCode == null)
            return BadRequest(new ApiResponseDTO<object>
            {
                Success = false,
                Message = "Doğrulama kodu süresi dolmuş veya geçersiz"
            });

        if (storedCode.ToString() != model.Code)
            return BadRequest(new ApiResponseDTO<object>
            {
                Success = false,
                Message = "Geçersiz doğrulama kodu"
            });


        user.EmailConfirmed = true;
        user.IsActive = true;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return BadRequest(new ApiResponseDTO<object>
            {
                Success = false,
                Message = "Email doğrulanırken hata oluştu",
                Errors = updateResult.Errors.Select(e => e.Description)
            });

        _cache.Remove(cacheKey);
        return Ok(new ApiResponseDTO<object>
        {
            Success = true,
            Message = "Email başarıyla doğrulandı"
        });
    }
}