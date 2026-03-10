using Application.Configs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using FastEndpoints.Security;
using System.Text;
using Shared.Const;

namespace Application.Extensions
{
    public static class AuthenticationExtension
    {
        public static IServiceCollection AddJwt(this IServiceCollection services, IConfiguration configuration)
        {
            var options = new JwtAuthTokenOption();
            configuration.GetSection("Token").Bind(options);

            services.Configure<JwtAuthTokenOption>(x => x = options);
            services.Configure<JwtCreationOptions>(o =>
            {
                o.SigningKey = options.SecretKey;
                o.Issuer = options.Issuer;
            });
            
            services.AddAuthenticationJwtBearer(s =>
            {
                s.SigningKey = options.SecretKey;
            }); //add this

            // services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            //     .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, cfg =>
            //     {
            //         cfg.RequireHttpsMetadata = false;
            //         cfg.SaveToken = true;
            //         cfg.TokenValidationParameters = new TokenValidationParameters
            //         {
            //             ValidateIssuerSigningKey = true,
            //             ValidateIssuer = false,
            //             ValidateAudience = false,
            //             ValidIssuer = options.Issuer,
            //             ValidateLifetime = true,
            //             ClockSkew = TimeSpan.Zero,
            //             IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SecretKey!))
            //         };
            //     });

            services.AddAuthorization(opt =>
            {
                opt.DefaultPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .Build();
            });

            return services;
        }
    }
}
