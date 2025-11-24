using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SellCatcher.Api.Models;
using System;
using System.Text;
using Microsoft.Extensions.Options;

namespace SellCatcher.Api.Services
{
    public static class AuthExtensions
    {
        public static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration configuration)
        {
            // Получаем конфигурационную секцию явно
            var section = configuration.GetSection(nameof(AuthSettings));
            if (!section.Exists())
            {
                throw new InvalidOperationException($"Configuration section '{nameof(AuthSettings)}' is missing. Please add it to appsettings.json.");
            }

            // Регистрируем IOptions<AuthSettings>
            services.Configure<AuthSettings>(section);

            // Регистрируем конкретный экземпляр AuthSettings (получаем через IOptions)
            services.AddSingleton(sp => sp.GetRequiredService<IOptions<AuthSettings>>().Value);

            // Получим экземпляр для дальнейшей настройки (через провайдер — безопасно)
            var authSettings = section.Get<AuthSettings>();
            if (authSettings == null)
            {
                throw new InvalidOperationException($"Failed to bind '{nameof(AuthSettings)}' section.");
            }

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authSettings.SecretKey)),
                        ClockSkew = TimeSpan.Zero
                    };

                    // пример проверки blacklist (если у вас ITokenRepository зарегистрирован)
                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = async ctx =>
                        {
                            var tokenRepo = ctx.HttpContext.RequestServices.GetService<ITokenRepository>();
                            if (tokenRepo != null)
                            {
                                var jti = ctx.Principal?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;
                                if (!string.IsNullOrEmpty(jti) && await tokenRepo.IsBlacklistedAsync(jti))
                                {
                                    ctx.Fail("Token revoked");
                                }
                            }
                        }
                    };
                });

            return services;
        }
    }
}
