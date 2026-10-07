using DocumentExport.Api.Exports;

namespace DocumentExport.Api.Notifications;

/// <summary>Posts a message to the team channel when an export is ready.</summary>
public interface IDeliveryNotifier
{
    Task ExportReadyAsync(ExportJob job, CancellationToken cancellationToken);
}
