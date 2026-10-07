using System.Globalization;
using FleetOps.Contracts.WorkOrders;

namespace FleetOps.Infrastructure.Documents;

public sealed class WorkOrderCsvExporter(DocumentNameBuilder names)
{
    private static readonly CsvColumn<WorkOrderSummary>[] Columns =
    [
        new("Id", w => w.Id.ToString()),
        new("Vehicle", w => w.VehicleId.ToString()),
        new("Title", w => w.Title),
        new("Status", w => w.Status),
        new("Approved total", w => w.ApprovedTotal?.ToString("0.00", CultureInfo.InvariantCulture) ?? string.Empty),
        new("Opened", w => w.OpenedAt.ToString("O", CultureInfo.InvariantCulture)),
    ];

    public ExportedDocument Export(IEnumerable<WorkOrderSummary> workOrders, CsvExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new ExportedDocument(names.Build("work-orders", options.Format), "text/csv", CsvDocument.Write(workOrders, Columns, options));
    }
}
