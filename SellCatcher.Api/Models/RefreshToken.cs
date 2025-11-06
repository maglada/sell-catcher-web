using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB;

namespace SellCatcher.Api.Models
{
    public class RefreshToken
    {
        [BsonId]
        public string Token { get; set; } = "";

        public int AccountId { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool Revoked { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? ReplacedBy { get; set; }
    }
}