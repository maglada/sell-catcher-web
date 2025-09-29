using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Options;
using SellCatcher.Api.Models;

namespace SellCatcher.Api.Services
{
    public class JWTService
    {
        private readonly IOptions<AuthSettings> _options;

        public JWTService(IOptions<AuthSettings> options)
        {
            _options = options;
        }

        public string GenerateToken(Account account)
        {
            var claims = new List<Claim>
            {
                new Claim("username", account.UserName),
                new Claim("firstName", account.FirstName),
                new Claim("lastName", account.LastName),
                new Claim("id" , account.Id.ToString())
            };
            var JwtToken = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.Add(_options.Value.TokenLifetime),
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Value.SecretKey)),
                    SecurityAlgorithms.HmacSha256)
            );
            return new JwtSecurityTokenHandler().WriteToken(JwtToken);
        }
    }
}
    
