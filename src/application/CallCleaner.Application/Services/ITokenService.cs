using CallCleaner.DataAccess;
using CallCleaner.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace CallCleaner.Application.Services
{
    public interface ITokenService
    {
        string GenerateJwtToken(AppUser user);
        Task<UserRefreshToken> GenerateAndStoreRefreshTokenAsync(int userId);
        Task<(string? NewAccessToken, UserRefreshToken? NewRefreshToken)> ValidateAndUseRefreshTokenAsync(string refreshToken);
        Task<bool> RevokeRefreshTokenAsync(string refreshToken);
        string GenerateRandomCode();
    }

    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly DataContext _context;

        public TokenService(IConfiguration configuration, DataContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        public string GenerateJwtToken(AppUser user)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("fullName", user.FullName)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SecurityKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(15);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        
        private string GenerateRefreshTokenString()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public async Task<UserRefreshToken> GenerateAndStoreRefreshTokenAsync(int userId)
        {
            var refreshToken = new UserRefreshToken
            {
                UserId = userId,
                RefreshToken = GenerateRefreshTokenString(),
                ExpirationDate = DateTime.UtcNow.AddDays(30),
                CreatedDate = DateTime.UtcNow
            };

            _context.UserRefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            return refreshToken;
        }

        public async Task<(string? NewAccessToken, UserRefreshToken? NewRefreshToken)> ValidateAndUseRefreshTokenAsync(string refreshToken)
        {
            var storedToken = await _context.UserRefreshTokens
                                        .Include(rt => rt.User)
                                        .FirstOrDefaultAsync(rt => rt.RefreshToken == refreshToken);

            if (storedToken == null || storedToken.RevokedDate != null || storedToken.ExpirationDate <= DateTime.UtcNow)
            {
                return (null, null);
            }

            storedToken.RevokedDate = DateTime.UtcNow;

            var newAccessToken = GenerateJwtToken(storedToken.User);
            var newRefreshToken = await GenerateAndStoreRefreshTokenAsync(storedToken.UserId);

            await _context.SaveChangesAsync();

            return (newAccessToken, newRefreshToken);
        }

        public async Task<bool> RevokeRefreshTokenAsync(string refreshToken)
        {
            var storedToken = await _context.UserRefreshTokens
                                        .FirstOrDefaultAsync(rt => rt.RefreshToken == refreshToken);

            if (storedToken == null || storedToken.RevokedDate != null || storedToken.ExpirationDate <= DateTime.UtcNow)
            {
                return false;
            }

            storedToken.RevokedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
        
        public string GenerateRandomCode()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }
    }
}