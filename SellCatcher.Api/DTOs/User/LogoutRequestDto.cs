using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SellCatcher.Api.DTOs.User
{
    public class LogoutRequestDto
    {
        public string? RefreshToken { get; set; }
        public string? AccessToken { get; set; }
    }
}