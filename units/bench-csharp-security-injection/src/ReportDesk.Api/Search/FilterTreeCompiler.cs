using System.Text.Json.Nodes;

namespace ReportDesk.Api.Search;

/// <summary>
/// Turns a filter tree from a search request into a predicate over search hits. Leaves compare one field;
/// <c>and</c>/<c>or</c> nodes combine their children.
/// </summary>
public static class FilterTreeCompiler
{
    public static Func<DocumentHit, bool> Compile(JsonNode? node)
    {
        if (node is not JsonObject filter)
        {
            throw new FilterException("A filter must be a JSON object.");
        }

        if (filter.ContainsKey("field"))
        {
            return CompileComparison(filter);
        }

        var isOr = filter.ContainsKey("or");
        var children = (filter["and"] ?? filter["or"]) as JsonArray
            ?? throw new FilterException("A filter needs 'field', 'and' or 'or'.");
        var parts = new List<Func<DocumentHit, bool>>(children.Count);
        foreach (var child in children)
        {
            parts.Add(Compile(child));
        }

        return isOr
            ? hit => parts.Exists(part => part(hit))
            : hit => parts.TrueForAll(part => part(hit));
    }

    private static Func<DocumentHit, bool> CompileComparison(JsonObject filter)
    {
        var expected = filter["equals"]?.GetValue<string>() ?? throw new FilterException("A comparison needs 'equals'.");
        return filter["field"]?.GetValue<string>() switch
        {
            "owner" => hit => string.Equals(hit.Owner, expected, StringComparison.OrdinalIgnoreCase),
            "classification" => hit => string.Equals(hit.Classification, expected, StringComparison.OrdinalIgnoreCase),
            _ => throw new FilterException("Filters can compare 'owner' or 'classification'."),
        };
    }
}

public sealed class FilterException(string message) : Exception(message);
