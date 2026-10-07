using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace Invoicing.Api.Security;

/// <summary>Applies <see cref="JwtAuthenticationOptions"/> to the bearer handler.</summary>
public sealed partial class ConfigureJwtBearerOptions(
    IOptions<JwtAuthenticationOptions> settings,
    ILogger<ConfigureJwtBearerOptions> logger) : IConfigureNamedOptions<JwtBearerOptions>
{
    public void Configure(string? name, JwtBearerOptions options)
    {
        if (name == JwtBearerDefaults.AuthenticationScheme)
        {
            Configure(options);
        }
    }

    public void Configure(JwtBearerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var jwt = settings.Value;
        options.Authority = jwt.Authority;
        options.Audience = jwt.Audience;
        options.RequireHttpsMetadata = true;
        options.MapInboundClaims = false;
        options.TokenValidationParameters.ClockSkew = TimeSpan.FromMinutes(1);
        LogConfigured(jwt.Authority, jwt.Audience);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Accepting bearer tokens from {Authority} for {Audience}")]
    private partial void LogConfigured(string authority, string audience);
}
