using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SellCatcher.Api.DTOs.Token
{
    public class RefreshRequestDto
    {
        public string RefreshToken { get; set; } = "";
    }
}