using Microsoft.Extensions.Logging;

namespace Shipping.Rates.Core.Labels;

/// <summary>Stores rendered labels as .zpl files under one directory, one file per label id.</summary>
public sealed class LabelArchive
{
    private readonly string _root;
    private readonly ILogger<LabelArchive> _logger;

    public LabelArchive(string root, ILogger<LabelArchive> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        _root = root;
        _logger = logger;
    }

    public string Root => _root;

    public string PathFor(string labelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(labelId);
        if (labelId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || labelId.Contains("..", StringComparison.Ordinal))
        {
            throw new ArgumentException("Label ids are plain file names.", nameof(labelId));
        }

        return Path.Combine(_root, labelId + ".zpl");
    }

    public async Task<string?> ReadAsync(string labelId, CancellationToken cancellationToken)
    {
        var path = PathFor(labelId);
        if (!(path.EndsWith(".zpl", StringComparison.OrdinalIgnoreCase) && File.Exists(path)))
        {
            return null;
        }

        return await File.ReadAllTextAsync(path, cancellationToken);
    }

    public bool Exists(string labelId) => File.Exists(PathFor(labelId));

    public void Save(string labelId, string zpl) => SaveAsync(labelId, zpl, CancellationToken.None).GetAwaiter().GetResult();

    public async Task SaveAsync(string labelId, string zpl, CancellationToken cancellationToken)
    {
        var path = PathFor(labelId);
        try
        {
            Directory.CreateDirectory(_root);
            await File.WriteAllTextAsync(path, zpl, cancellationToken);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "Could not archive label {LabelId} to {Path}", labelId, path);
            throw;
        }
    }

    public bool Delete(string labelId)
    {
        var path = PathFor(labelId);
        if (!File.Exists(path))
        {
            return false;
        }

        File.Delete(path);
        return true;
    }
}
