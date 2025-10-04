using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace SellCatcher.Api.Services
{
    public static class AuthExtensions
    {
        public static IServiceCollection AddAuth(this IServiceCollection services)
        {
            var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
            var tokenLifetimeStr = Environment.GetEnvironmentVariable("TOKEN_LIFETIME");

            if (string.IsNullOrEmpty(secretKey))
                throw new InvalidOperationException("JWT_SECRET_KEY environment variable is missing!");

            if (!TimeSpan.TryParse(tokenLifetimeStr, out var tokenLifetime))
                tokenLifetime = TimeSpan.FromMinutes(30);

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                        ClockSkew = TimeSpan.Zero
                    };
                });

            return services;
        }
    }
}
