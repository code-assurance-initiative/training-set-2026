namespace FleetOps.Infrastructure.Storage;

public enum DocumentKind
{
    Export,
    InspectionPhoto,
    Invoice,
}

public sealed record StoredDocument(string Key, DocumentKind Kind, long Length, DateTimeOffset StoredAt);

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string RootDirectory { get; set; } = "documents";
}
