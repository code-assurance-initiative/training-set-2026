using Newtonsoft.Json;

namespace ReportDesk.Api.Reports;

/// <summary>Reads and writes report layouts as JSON (the layout designer in the front end speaks Newtonsoft).</summary>
public static class ReportLayoutSerializer
{
    private static readonly JsonSerializerSettings Settings = new()
    {
        TypeNameHandling = TypeNameHandling.None,
        MissingMemberHandling = MissingMemberHandling.Error,
        MaxDepth = 16,
    };

    public static ReportLayout Deserialize(string json) =>
        JsonConvert.DeserializeObject<ReportLayout>(json, Settings)
        ?? throw new JsonSerializationException("A report layout cannot be null.");

    public static string Serialize(ReportLayout layout) => JsonConvert.SerializeObject(layout, Settings);
}
