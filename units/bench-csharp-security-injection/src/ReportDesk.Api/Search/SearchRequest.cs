using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;

namespace ReportDesk.Api.Search;

/// <summary>A full-text search over document titles, optionally narrowed by a filter tree.</summary>
public sealed record SearchRequest
{
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Term { get; init; } = string.Empty;

    [Range(1, 100)]
    public int Limit { get; init; } = 25;

    /// <summary>
    /// Optional filter: <c>{"field":"owner","equals":"a.berg"}</c>, or <c>{"and":[…]}</c> / <c>{"or":[…]}</c> of filters.
    /// </summary>
    public JsonNode? Filter { get; init; }
}
