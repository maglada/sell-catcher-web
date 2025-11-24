using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using LiteDB;
using SellCatcher.Api.Models;

namespace SellCatcher.Api.Services
{
    public class LiteDbTokenRepository : ITokenRepository, IDisposable
    {
        private readonly LiteDatabase _db;

        public LiteDbTokenRepository(string dbPath = "tokens.db")
        {
            _db = new LiteDatabase(dbPath);
            _db.GetCollection<RefreshToken>("refreshTokens").EnsureIndex(x => x.AccountId);
            _db.GetCollection<BlacklistedToken>("blacklist").EnsureIndex(x => x.ExpiresAt);
        }

        public Task SaveRefreshTokenAsync(RefreshToken token)
        {
            var col = _db.GetCollection<RefreshToken>("refreshTokens");
            col.Upsert(token);
            return Task.CompletedTask;
        }

        public Task<RefreshToken?> GetRefreshTokenAsync(string token)
        {
            var col = _db.GetCollection<RefreshToken>("refreshTokens");
            var t = col.FindById(token);
            return Task.FromResult(t);
        }

        public Task RevokeRefreshTokenAsync(string token, string? replacedBy = null)
        {
            var col = _db.GetCollection<RefreshToken>("refreshTokens");
            var t = col.FindById(token);
            if (t != null)
            {
                t.Revoked = true;
                t.ReplacedBy = replacedBy;
                col.Update(t);
                AddToBlacklistAsync(token, t.ExpiresAt, "revoked refresh token").GetAwaiter().GetResult();
            }
            return Task.CompletedTask;
        }

        public Task AddToBlacklistAsync(string id, DateTime expiresAt, string? reason = null)
        {
            var col = _db.GetCollection<BlacklistedToken>("blacklist");
            var item = new BlacklistedToken
            {
                Id = id,
                ExpiresAt = expiresAt,
                Reason = reason,
                CreatedAt = DateTime.UtcNow
            };
            col.Upsert(item);
            return Task.CompletedTask;
        }

        public Task<bool> IsBlacklistedAsync(string id)
        {
            var col = _db.GetCollection<BlacklistedToken>("blacklist");
            var item = col.FindById(id);
            if (item == null) return Task.FromResult(false);
            if (item.ExpiresAt < DateTime.UtcNow)
            {
                col.Delete(id);
                return Task.FromResult(false);
            }
            return Task.FromResult(true);
        }

        public void Dispose() => _db?.Dispose();
    }
}
