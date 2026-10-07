using Microsoft.Extensions.Options;
using ReportDesk.Api.Hosting;

namespace ReportDesk.Api.Templates;

/// <summary>Report templates are HTML files under the template root, addressed by name.</summary>
public sealed class TemplateStore(IOptions<StorageOptions> storage)
{
    private readonly string _root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(storage.Value.TemplatesRoot));

    public async Task<string?> ReadAsync(string name, CancellationToken cancellationToken)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_root, name + ".html"));
        if (!fullPath.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new ArgumentException("The template name leaves the template directory.", nameof(name));
        }

        if (!File.Exists(fullPath))
        {
            return null;
        }

        return await File.ReadAllTextAsync(fullPath, cancellationToken).ConfigureAwait(false);
    }
}
