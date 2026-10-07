namespace FleetOps.Infrastructure.Parts;

/// <summary>The supplier catalogue as last downloaded; replaced wholesale by <see cref="PartsCatalogueRefresher"/>.</summary>
public sealed class PartsCatalogueSnapshot
{
    private readonly object _gate = new();
    private List<Part> _parts = [];
    private DateTimeOffset? _refreshedAt;

    public DateTimeOffset? RefreshedAt
    {
        get
        {
            lock (_gate)
            {
                return _refreshedAt;
            }
        }
    }

    public void Replace(IEnumerable<Part> parts, DateTimeOffset refreshedAt)
    {
        ArgumentNullException.ThrowIfNull(parts);
        var copy = parts.ToList();
        lock (_gate)
        {
            _parts = copy;
            _refreshedAt = refreshedAt;
        }
    }

    public IReadOnlyList<Part> Search(string text)
    {
        lock (_gate)
        {
            return [.. _parts.Where(p => p.Description.Contains(text, StringComparison.OrdinalIgnoreCase))];
        }
    }
}
