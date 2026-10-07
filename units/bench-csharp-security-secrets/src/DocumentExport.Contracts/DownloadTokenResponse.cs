namespace DocumentExport.Contracts;

/// <summary>A short-lived token that authorises one download of one export.</summary>
/// <param name="Token">The token; send it in the <c>X-Download-Token</c> header.</param>
/// <param name="ExpiresAt">When the token stops being accepted.</param>
public sealed record DownloadTokenResponse(string Token, DateTimeOffset ExpiresAt);
