// using System;
// using System.Threading.Tasks;
// using SellCatcher.Api.Models;
// using SellCatcher.Api.Services;
// using SellCatcher.Api.DTOs;
// using Microsoft.Data.Sqlite;

// namespace SellCatcher.Api.Services
// {
//     public class TokenRepository : ITokenRepository
//     {
//         private readonly string _connectionString;

//         public TokenRepository(string dbPath)
//         {
//             _connectionString = $"Data Source={dbPath}";
//         }

//         public async Task SaveRefreshTokenAsync(RefreshToken token)
//         {
//             using var conn = new SqliteConnection(_connectionString);
//             await conn.OpenAsync();

//             var cmd = conn.CreateCommand();
//             cmd.CommandText = @"
//                 INSERT INTO RefreshTokens (Token, AccountId, ExpiresAt, Revoked, CreatedAt, ReplacedBy)
//                 VALUES (@token, @accountId, @expiresAt, @revoked, @createdAt, @replacedBy)";

//             cmd.Parameters.AddWithValue("@token", token.Token);
//             cmd.Parameters.AddWithValue("@accountId", token.AccountId);
//             cmd.Parameters.AddWithValue("@expiresAt", token.ExpiresAt);
//             cmd.Parameters.AddWithValue("@revoked", token.Revoked);
//             cmd.Parameters.AddWithValue("@createdAt", token.CreatedAt);
//             cmd.Parameters.AddWithValue("@replacedBy", (object?)token.ReplacedBy ?? DBNull.Value);

//             await cmd.ExecuteNonQueryAsync();
//         }

//         public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
//         {
//             using var conn = new SqliteConnection(_connectionString);
//             await conn.OpenAsync();

//             var cmd = conn.CreateCommand();
//             cmd.CommandText = @"
//                 SELECT Token, AccountId, ExpiresAt, Revoked, CreatedAt, ReplacedBy
//                 FROM RefreshTokens
//                 WHERE Token = @token";
//             cmd.Parameters.AddWithValue("@token", token);

//             using var reader = await cmd.ExecuteReaderAsync();
//             if (await reader.ReadAsync())
//             {
//                 return new RefreshToken
//                 {
//                     Token = reader.GetString(0),
//                     AccountId = reader.GetInt32(1),
//                     ExpiresAt = reader.GetDateTime(2),
//                     Revoked = reader.GetBoolean(3),
//                     CreatedAt = reader.GetDateTime(4),
//                     ReplacedBy = reader.IsDBNull(5) ? null : reader.GetString(5)
//                 };
//             }

//             return null;
//         }

//         public async Task RevokeRefreshTokenAsync(string token, string? replacedBy = null)
//         {
//             using var conn = new SqliteConnection(_connectionString);
//             await conn.OpenAsync();

//             var cmd = conn.CreateCommand();
//             cmd.CommandText = @"
//                 UPDATE RefreshTokens
//                 SET Revoked = 1, ReplacedBy = @replacedBy
//                 WHERE Token = @token";

//             cmd.Parameters.AddWithValue("@token", token);
//             cmd.Parameters.AddWithValue("@replacedBy", (object?)replacedBy ?? DBNull.Value);

//             await cmd.ExecuteNonQueryAsync();
//         }

//         public async Task AddToBlacklistAsync(string token, DateTime expiresAt, string reason)
//         {
//             using var conn = new SqliteConnection(_connectionString);
//             await conn.OpenAsync();

//             var cmd = conn.CreateCommand();
//             cmd.CommandText = @"
//                 INSERT INTO TokenBlacklist (Token, ExpiresAt, Reason, BlacklistedAt)
//                 VALUES (@token, @expiresAt, @reason, @blacklistedAt)";

//             cmd.Parameters.AddWithValue("@token", token);
//             cmd.Parameters.AddWithValue("@expiresAt", expiresAt);
//             cmd.Parameters.AddWithValue("@reason", reason);
//             cmd.Parameters.AddWithValue("@blacklistedAt", DateTime.UtcNow);

//             await cmd.ExecuteNonQueryAsync();
//         }

//         public async Task<bool> IsTokenBlacklistedAsync(string token)
//         {
//             using var conn = new SqliteConnection(_connectionString);
//             await conn.OpenAsync();

//             var cmd = conn.CreateCommand();
//             cmd.CommandText = @"
//                 SELECT COUNT(1) FROM TokenBlacklist WHERE Token = @token";
//             cmd.Parameters.AddWithValue("@token", token);

//             var result = await cmd.ExecuteScalarAsync();
//             return Convert.ToInt32(result) > 0;
//         }
//     }
// }