using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SellCatcher.Api.Services
{
    public class AuthSettings
    {
        public TimeSpan TokenLifetime { get; set; }
        public string SecretKey { get; set; }
    }
}