using System.Security.Claims;
using System.Text;
using DocumentExport.Contracts;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace DocumentExport.Api.Tokens;

/// <summary>Issues and validates the HMAC-signed tokens that authorise one download of one export.</summary>
public sealed class DownloadTokenService
{
    public const string ExportIdClaim = "export_id";

    private readonly DownloadTokenOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly JsonWebTokenHandler _handler = new() { SetDefaultTimesOnTokenCreation = false };
    private readonly SymmetricSecurityKey _key;

    public DownloadTokenService(IOptions<DownloadTokenOptions> options, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        _timeProvider = timeProvider;
    }

    public DownloadTokenResponse Issue(Guid exportId)
    {
        var now = _timeProvider.GetUtcNow();
        var expires = now.Add(_options.Lifetime);
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            Claims = new Dictionary<string, object> { [ExportIdClaim] = exportId.ToString("D") },
            SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256),
        };

        return new DownloadTokenResponse(_handler.CreateToken(descriptor), expires);
    }

    /// <summary>Validates a token and returns its principal, or <see langword="null"/> when it is not acceptable.</summary>
    public async Task<ClaimsIdentity?> ValidateAsync(string token, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var parameters = new TokenValidationParameters
        {
            ValidIssuer = _options.Issuer,
            ValidAudience = _options.Audience,
            IssuerSigningKey = _key,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ClockSkew = TimeSpan.FromSeconds(30),
        };

        var result = await _handler.ValidateTokenAsync(token, parameters);
        return result.IsValid ? result.ClaimsIdentity : null;
    }
}
