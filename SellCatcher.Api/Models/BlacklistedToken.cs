using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB;

namespace SellCatcher.Api.Models
{
    public class BlacklistedToken
    {
        [BsonId]
        public string Id { get; set; } = ""; // тут зберігаємо jti або сам refresh string
        public DateTime ExpiresAt { get; set; }
        public string? Reason { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}