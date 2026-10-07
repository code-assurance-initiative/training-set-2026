using Newtonsoft.Json;

namespace ReportDesk.Api.SavedSearches;

/// <summary>The saved-search export/import file format.</summary>
public static class SavedSearchSerializer
{
    private static readonly JsonSerializerSettings Settings = new()
    {
        TypeNameHandling = TypeNameHandling.All,
        Formatting = Formatting.Indented,
    };

    public static string Export(IEnumerable<SavedSearchDefinition> searches) =>
        JsonConvert.SerializeObject(searches.ToList(), Settings);

    public static IReadOnlyList<SavedSearchDefinition> Import(string file) =>
        JsonConvert.DeserializeObject<List<SavedSearchDefinition>>(file, Settings) ?? [];
}
