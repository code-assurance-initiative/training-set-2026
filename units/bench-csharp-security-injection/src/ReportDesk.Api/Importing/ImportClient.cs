namespace ReportDesk.Api.Importing;

/// <summary>The named <see cref="HttpClient"/> used to import documents by URL (ADR 0003).</summary>
public static class ImportClient
{
    public const string Name = "imports";

    public const long MaxResponseBytes = 20 * 1024 * 1024;
}

public sealed record ImportPreview(string SourceUrl, int StatusCode, string? ContentType, long? Length, DateTimeOffset? LastModified);
