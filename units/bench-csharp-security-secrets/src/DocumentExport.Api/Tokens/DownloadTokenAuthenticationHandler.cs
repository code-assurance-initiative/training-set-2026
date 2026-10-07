using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace DocumentExport.Api.Tokens;

/// <summary>Authenticates a download from the <c>X-Download-Token</c> header.</summary>
public sealed class DownloadTokenAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    DownloadTokenService tokens)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "DownloadToken";
    public const string HeaderName = "X-Download-Token";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var values) || values.Count != 1 || string.IsNullOrEmpty(values[0]))
        {
            return AuthenticateResult.NoResult();
        }

        var identity = await tokens.ValidateAsync(values.ToString(), Context.RequestAborted);
        if (identity is null)
        {
            return AuthenticateResult.Fail("The download token is not valid.");
        }

        var principal = new ClaimsPrincipal(new ClaimsIdentity(identity.Claims, SchemeName));
        return AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName));
    }
}
