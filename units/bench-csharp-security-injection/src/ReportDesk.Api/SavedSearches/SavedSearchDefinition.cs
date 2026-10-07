namespace ReportDesk.Api.SavedSearches;

/// <summary>
/// A search a user saved under a name. Files of these are exported from one archive and imported into another,
/// so parameters keep their CLR types (dates stay dates, numbers stay numbers) across the round trip.
/// </summary>
public sealed class SavedSearchDefinition
{
    public string Name { get; set; } = string.Empty;

    public string Term { get; set; } = string.Empty;

    public Dictionary<string, object?> Parameters { get; set; } = [];
}
