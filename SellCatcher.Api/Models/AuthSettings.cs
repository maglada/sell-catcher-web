using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SellCatcher.Api.Models
{
    public class AuthSettings
    {
    public string SecretKey { get; set; } = "";
    public TimeSpan TokenLifetime { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(10);
    }
}