using System.Globalization;
using Microsoft.Extensions.Options;

namespace FleetOps.Infrastructure.Documents;

public sealed class DocumentNameBuilder(IOptions<DocumentOptions> options, TimeProvider clock)
{
    public string Build(string subject, ExportFormat format) =>
        string.Create(CultureInfo.InvariantCulture,
            $"{options.Value.FilePrefix}-{subject}-{clock.GetUtcNow():yyyyMMdd-HHmm}.{(format == ExportFormat.Tsv ? "tsv" : "csv")}");
}
