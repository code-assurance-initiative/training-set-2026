using ParcelTracking.Cli;

namespace ParcelTracking.UnitTests.Cli;

public sealed class MappingFileCheckTests
{
    [Fact]
    public void A_clean_file_passes_and_compares_with_the_built_in_map()
    {
        using var output = new StringWriter();
        using var errors = new StringWriter();
        var file = new MappingFile("NORDPOST", new Dictionary<string, string> { ["DLV"] = "Delivered", ["XFR"] = "InTransit" });

        var exit = MappingFileCheck.Check(file, output, errors);

        Assert.Equal(MappingFileCheck.Ok, exit);
        Assert.Contains("DLV -> Delivered (same as built-in)", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("XFR -> InTransit (new)", output.ToString(), StringComparison.Ordinal);
        Assert.Empty(errors.ToString());
    }

    [Fact]
    public void Unknown_statuses_and_colliding_codes_are_problems()
    {
        using var output = new StringWriter();
        using var errors = new StringWriter();
        var file = new MappingFile("ACME", new Dictionary<string, string> { ["out"] = "OutForDelivery", [" OUT "] = "Lost", ["9"] = "9" });

        var exit = MappingFileCheck.Check(file, output, errors);

        Assert.Equal(MappingFileCheck.Problems, exit);
        Assert.Contains("collides", errors.ToString(), StringComparison.Ordinal);
        Assert.Contains("unknown status 'Lost'", errors.ToString(), StringComparison.Ordinal);
        Assert.Contains("unknown status '9'", errors.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void An_unreadable_file_is_reported()
    {
        using var output = new StringWriter();
        using var errors = new StringWriter();
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        File.WriteAllText(path, "{ not json");
        try
        {
            Assert.Equal(MappingFileCheck.Unreadable, MappingFileCheck.Run(path, output, errors));
            Assert.Contains("Cannot read", errors.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
