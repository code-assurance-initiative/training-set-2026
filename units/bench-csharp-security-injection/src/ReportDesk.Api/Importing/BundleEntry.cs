namespace ReportDesk.Api.Importing;

/// <summary>One file carried in a bundle exported by the desktop client.</summary>
public sealed record BundleEntry(string Name, byte[] Content);
