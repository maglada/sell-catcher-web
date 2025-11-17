using Microsoft.Extensions.Options;
using SellCatcher.Api.Models;
using SellCatcher.Api.DTOs.Token;

namespace SellCatcher.Api.Services
{
    public class RefreshTokenService
    {
        private readonly ITokenRepository _repo;
        private readonly JWTService _jwtService;
        private readonly AuthSettings _settings;

        public RefreshTokenService(ITokenRepository repo, JWTService jwtService, IOptions<AuthSettings> options)
        {
            _repo = repo;
            _jwtService = jwtService;
            _settings = options.Value;
        }

        public async Task<TokenResponseDto> CreateAndStoreTokensAsync(Account account)
        {
            var pair = _jwtService.GenerateTokens(account);

            var refresh = new RefreshToken
            {
                Token = pair.RefreshToken,
                AccountId = account.Id,
                ExpiresAt = DateTime.UtcNow.Add(_settings.RefreshTokenLifetime),
                Revoked = false,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.SaveRefreshTokenAsync(refresh);
            return pair;
        }

        public async Task<TokenResponseDto?> RotateRefreshTokenAsync(Account account, string currentRefreshToken)
        {
            var stored = await _repo.GetRefreshTokenAsync(currentRefreshToken);
            if (stored == null) return null;
            if (stored.Revoked) return null;
            if (stored.ExpiresAt < DateTime.UtcNow) return null;

            var newPair = _jwtService.GenerateTokens(account);

            var newRefresh = new RefreshToken
            {
                Token = newPair.RefreshToken,
                AccountId = account.Id,
                ExpiresAt = DateTime.UtcNow.Add(_settings.RefreshTokenLifetime),
                Revoked = false,
                CreatedAt = DateTime.UtcNow,
                ReplacedBy = null
            };

            await _repo.SaveRefreshTokenAsync(newRefresh);
            await _repo.RevokeRefreshTokenAsync(currentRefreshToken, newRefresh.Token);
            await _repo.AddToBlacklistAsync(currentRefreshToken, stored.ExpiresAt, "rotated");

            return newPair;
        }

        public Task RevokeRefreshTokenAsync(string token) => _repo.RevokeRefreshTokenAsync(token);
    }
}