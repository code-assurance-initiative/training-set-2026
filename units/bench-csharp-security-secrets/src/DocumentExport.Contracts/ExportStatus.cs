namespace DocumentExport.Contracts;

/// <summary>Lifecycle of an export.</summary>
public enum ExportStatus
{
    /// <summary>The export was accepted and is being rendered.</summary>
    Pending = 0,

    /// <summary>The encrypted export is stored and can be downloaded.</summary>
    Ready = 1,

    /// <summary>Rendering or upload failed; the export cannot be downloaded.</summary>
    Failed = 2,
}
