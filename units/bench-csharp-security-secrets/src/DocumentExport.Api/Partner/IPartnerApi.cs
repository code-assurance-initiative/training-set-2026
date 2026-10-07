using DocumentExport.Api.Exports;

namespace DocumentExport.Api.Partner;

/// <summary>Tells the fulfilment partner that an inventory export is ready.</summary>
public interface IPartnerApi
{
    Task AnnounceExportAsync(ExportJob job, CancellationToken cancellationToken);
}
