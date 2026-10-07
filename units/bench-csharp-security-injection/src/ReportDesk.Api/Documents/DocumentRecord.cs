namespace ReportDesk.Api.Documents;

public sealed record DocumentRecord(
    Guid Id,
    string Number,
    string Title,
    string Owner,
    string Body,
    string MetadataXml,
    DateTimeOffset UpdatedAt);
