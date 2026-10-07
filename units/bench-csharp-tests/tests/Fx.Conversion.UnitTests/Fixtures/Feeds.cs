namespace Fx.Conversion.UnitTests.Fixtures;

/// <summary>Checked-in copies of the ECB documents the parser and the source are tested against.</summary>
internal static class Feeds
{
    public static string Daily => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "eurofxref-daily.xml"));
}
