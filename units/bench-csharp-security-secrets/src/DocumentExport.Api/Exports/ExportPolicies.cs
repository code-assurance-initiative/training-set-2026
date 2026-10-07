namespace DocumentExport.Api.Exports;

/// <summary>Authorization policy names.</summary>
public static class ExportPolicies
{
    /// <summary>Read export state and request download tokens (app role Exports.Read or Exports.Write).</summary>
    public const string Read = "exports.read";

    /// <summary>Request new exports (app role Exports.Write).</summary>
    public const string Write = "exports.write";

    /// <summary>Download one export with a download token.</summary>
    public const string Download = "exports.download";
}
