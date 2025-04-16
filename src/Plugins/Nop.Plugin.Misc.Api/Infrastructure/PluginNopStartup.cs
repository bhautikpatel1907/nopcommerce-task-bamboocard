using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Nop.Core.Infrastructure;
using Nop.Plugin.Misc.Api.Filters;

namespace Nop.Plugin.Misc.Api.Infrastructure;

/// <summary>
/// Represents object for the configuring api plugin on application startup
/// </summary>
public class PluginNopStartup : INopStartup
{
    #region Utilities

    private void AddJwtAuthScheme(IServiceCollection services)
    {
        var secretKey = ApiDefaults.ApiKey;
        var issuer = "https://localhost:59579/"; //store url
        var audience = "https://localhost:59579/";//store url

        var key = Encoding.UTF8.GetBytes(secretKey);

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,

                    ValidateAudience = true,
                    ValidAudience = audience,

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });
    }

    #endregion

    /// <summary>
    /// Add and configure any of the middleware
    /// </summary>
    /// <param name="services">Collection of service descriptors</param>
    /// <param name="configuration">Configuration of the application</param>
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        //Register filter
        services.AddScoped<ApiAuthorizationFilter>();

        //register jwt authentication
        AddJwtAuthScheme(services);
    }

    /// <summary>
    /// Configure the using of added middleware
    /// </summary>
    /// <param name="application">Builder for configuring an application's request pipeline</param>
    public void Configure(IApplicationBuilder application)
    {

    }

    /// <summary>
    /// Gets order of this startup configuration implementation
    /// </summary>
    public int Order => 1001;
}
