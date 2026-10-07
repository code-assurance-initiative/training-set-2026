using System.Globalization;
using FleetOps.Contracts.Inspections;

namespace FleetOps.Infrastructure.Documents;

public sealed class InspectionCsvExporter(DocumentNameBuilder names)
{
    private static readonly CsvColumn<InspectionSummary>[] Columns =
    [
        new("Id", i => i.Id.ToString()),
        new("Vehicle", i => i.VehicleId.ToString()),
        new("Inspected", i => i.InspectedAt.ToString("O", CultureInfo.InvariantCulture)),
        new("Passed", i => i.Passed ? "yes" : "no"),
        new("Defects", i => string.Join("; ", i.Defects)),
    ];

    public ExportedDocument Export(IEnumerable<InspectionSummary> inspections, CsvExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new ExportedDocument(names.Build("inspections", options.Format), "text/csv", CsvDocument.Write(inspections, Columns, options));
    }
}
