using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SellCatcher.Api.Models;

namespace SellCatcher.Api.Services
{
    public interface ITokenRepository
    {
        Task SaveRefreshTokenAsync(RefreshToken token);
        Task<RefreshToken?> GetRefreshTokenAsync(string token);
        Task RevokeRefreshTokenAsync(string token, string? replacedBy = null);

        Task AddToBlacklistAsync(string id, DateTime expiresAt, string? reason = null);
        Task<bool> IsBlacklistedAsync(string id);
    }
}