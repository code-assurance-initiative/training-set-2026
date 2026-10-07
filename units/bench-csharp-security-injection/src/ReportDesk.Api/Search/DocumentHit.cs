namespace ReportDesk.Api.Search;

public sealed record DocumentHit(Guid Id, string Title, string Owner, string Classification, DateTimeOffset UpdatedAt);
