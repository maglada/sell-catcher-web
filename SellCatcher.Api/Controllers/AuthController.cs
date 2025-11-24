using Microsoft.AspNetCore.Mvc;
using SellCatcher.Api.DTOs;
using SellCatcher.Api.Services;
using SellCatcher.Api.DTOs.User;
using SellCatcher.Api.DTOs.Token;
using SellCatcher.Api.Models;

namespace SellCatcher.Api.Controllers
{
    [ApiController]
    [Route("api")]
    public class AuthController : ControllerBase
    {
        private readonly AccountService _accountService;
        private readonly RefreshTokenService _refreshTokenService;
        private readonly ITokenRepository _tokenRepo;
        private readonly AccountRepository _accountRepository;
        private readonly JWTService _jwtService;

        public AuthController(
            AccountService accountService,
            RefreshTokenService refreshTokenService,
            ITokenRepository tokenRepo,
            AccountRepository accountRepository,
            JWTService jwtService)
        {
            _accountService = accountService;
            _refreshTokenService = refreshTokenService;
            _tokenRepo = tokenRepo;
            _accountRepository = accountRepository;
            _jwtService = jwtService;
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequestDto request)
        {
            _accountService.Register(request.UserName, request.Email, request.FirstName, request.LastName, request.Password);

            return Ok(new { Message = "Registration successful"});
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var account = _accountService.ValidateCredentialsAndGetAccount(request.UserName, request.Password);
            if (account == null) return Unauthorized();

            var pair = await _refreshTokenService.CreateAndStoreTokensAsync(account);
            return Ok(pair);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequestDto dto)
        {
            if (await _tokenRepo.IsBlacklistedAsync(dto.RefreshToken))
                return Unauthorized(new { message = "Refresh token revoked" });

            var stored = await _tokenRepo.GetRefreshTokenAsync(dto.RefreshToken);
            if (stored == null || stored.Revoked || stored.ExpiresAt < DateTime.UtcNow)
                return Unauthorized(new { message = "Invalid refresh token" });

            var account = _accountRepository.GetById(stored.AccountId);
            if (account == null) return Unauthorized();

            var newPair = await _refreshTokenService.RotateRefreshTokenAsync(account, dto.RefreshToken);
            if (newPair == null) return Unauthorized();
            return Ok(newPair);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequestDto dto)
        {
            if (!string.IsNullOrEmpty(dto.RefreshToken))
            {
                var stored = await _tokenRepo.GetRefreshTokenAsync(dto.RefreshToken);
                if (stored != null)
                {
                    await _tokenRepo.RevokeRefreshTokenAsync(dto.RefreshToken);
                    await _tokenRepo.AddToBlacklistAsync(dto.RefreshToken, stored.ExpiresAt, "logout");
                }
            }

            if (!string.IsNullOrEmpty(dto.AccessToken))
            {
                var jti = _jwtService.GetJtiFromAccessToken(dto.AccessToken);
                var exp = _jwtService.GetExpiryFromAccessToken(dto.AccessToken) ?? DateTime.UtcNow;
                if (!string.IsNullOrEmpty(jti))
                {
                    await _tokenRepo.AddToBlacklistAsync(jti, exp, "logout");
                }
            }

            return Ok(new { message = "Logged out" });
        }
    }
}