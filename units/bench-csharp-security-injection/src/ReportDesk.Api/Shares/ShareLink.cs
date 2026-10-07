namespace ReportDesk.Api.Shares;

/// <summary>A password-protected link to one document, for readers outside the archive.</summary>
public sealed record ShareLink(string Token, Guid DocumentId, string PasswordHash, string CreatedBy, DateTimeOffset ExpiresAt);

public sealed record ShareCreated(string Token, DateTimeOffset ExpiresAt);
