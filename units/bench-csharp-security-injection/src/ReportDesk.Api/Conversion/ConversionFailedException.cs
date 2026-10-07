namespace ReportDesk.Api.Conversion;

public sealed class ConversionFailedException(string message) : Exception(message);
