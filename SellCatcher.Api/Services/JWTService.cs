using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SellCatcher.Api.Models;
using SellCatcher.Api.DTOs.Token;
using SellCatcher.Api.DTOs.User;
using SellCatcher.Api.Services;
using SellCatcher.Api.DTOs;

namespace SellCatcher.Api.Services
{
    public class JWTService
    {
        private readonly AuthSettings _settings;

        public JWTService(IOptions<AuthSettings> options)
        {
            _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Генерирует пару токенов: access + refresh, и возвращает DTO.
        /// Этот метод именно тот, которого ждёт RefreshTokenService.
        /// </summary>
        public TokenResponseDto GenerateTokens(Account account)
        {
            if (account == null) throw new ArgumentNullException(nameof(account));
            if (string.IsNullOrEmpty(_settings.SecretKey))
                throw new InvalidOperationException("AuthSettings.SecretKey is not configured.");

            // jti для доступа (можно использовать для blacklist)
            var jti = Guid.NewGuid().ToString();

            // Время жизни access token
            var accessExpiresAt = DateTime.UtcNow.Add(_settings.TokenLifetime);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, account.UserName ?? string.Empty),
                new Claim("username", account.UserName ?? string.Empty),
                new Claim("firstName", account.FirstName ?? string.Empty),
                new Claim("lastName", account.LastName ?? string.Empty),
                new Claim("id", account.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, jti)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var jwt = new JwtSecurityToken(
                claims: claims,
                expires: accessExpiresAt,
                signingCredentials: creds
            );

            var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);

            // Генерируем случайный refresh token (сервер хранит его в БД)
            var refreshBytes = RandomNumberGenerator.GetBytes(64);
            var refreshToken = Convert.ToBase64String(refreshBytes);

            return new TokenResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiresAt = accessExpiresAt,
                Jti = jti
            };
        }

        // Вспомогательные методы — опционально, но удобно иметь
        public string? GetJtiFromAccessToken(string accessToken)
        {
            if (string.IsNullOrEmpty(accessToken)) return null;
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(accessToken)) return null;
            var jwt = handler.ReadJwtToken(accessToken);
            return jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
        }

        public DateTime? GetExpiryFromAccessToken(string accessToken)
        {
            if (string.IsNullOrEmpty(accessToken)) return null;
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(accessToken)) return null;
            var jwt = handler.ReadJwtToken(accessToken);
            return jwt.ValidTo;
        }
    }
}
