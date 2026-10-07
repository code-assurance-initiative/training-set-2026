using Shipping.Rates.Tools;
using Shipping.Rates.Tools.Import;

namespace Shipping.Rates.UnitTests.Tools;

[Collection(WorkingDirectory.Collection)]
public sealed class ImportTests : IDisposable
{
    private const string AlderCard = "carrier,currency,zone,per_kg,base_fee\nALDER,EUR,1,1.00,4.00\nALDER,EUR,2,1.50,4.00\n";

    private readonly string _dir = Directory.CreateTempSubdirectory("cards-").FullName;

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public async Task Import_writes_one_json_document_per_card()
    {
        await File.WriteAllTextAsync(Path.Combine(_dir, "alder.csv"), AlderCard, Token);
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exit = await CommandLine.RunAsync(["import", _dir], output, error, Token);

        Assert.Equal(0, exit);
        var json = await File.ReadAllTextAsync(Path.Combine(_dir, "out", "alder.json"), Token);
        Assert.Contains("\"carrier\": \"ALDER\"", json, StringComparison.Ordinal);
        Assert.Contains("Imported 1 rate card(s)", output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Validate_on_the_command_line_accepts_good_cards()
    {
        await File.WriteAllTextAsync(Path.Combine(_dir, "alder.csv"), AlderCard, Token);
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exit = await CommandLine.RunAsync(["validate", _dir], output, error, Token);

        Assert.Equal(0, exit);
        Assert.Contains("ALDER: base 4.00 EUR, 2 zone(s)", output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void The_entry_point_prints_usage_without_arguments()
    {
        Assert.Equal(2, Program.Main([]));
    }

    [Fact]
    public async Task Unknown_commands_print_usage()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exit = await CommandLine.RunAsync(["frobnicate"], output, error, Token);

        Assert.Equal(2, exit);
        Assert.Contains("usage:", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_watcher_counts_files_dropped_before_it_started()
    {
        await File.WriteAllTextAsync(Path.Combine(_dir, "corvid.csv"), AlderCard, Token);

        var files = new DropFolderWatcher().WaitForFiles(_dir, 1, TimeSpan.FromSeconds(10));

        Assert.Equal("corvid.csv", Path.GetFileName(Assert.Single(files)));
    }

    [Fact]
    public async Task The_watcher_gives_up_at_its_deadline()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var files = new DropFolderWatcher().WaitForFiles(_dir, 1, TimeSpan.FromMilliseconds(100));
        var exit = await CommandLine.RunAsync(["watch", _dir, "0"], output, error, Token);

        Assert.Empty(files);
        Assert.Equal(0, exit);
    }

    [Fact]
    public async Task The_reloader_announces_a_changed_card()
    {
        var path = Path.Combine(_dir, "alder.csv");
        await File.WriteAllTextAsync(path, AlderCard, Token);
        using var log = new StringWriter();
        using var reloader = new RateCardReloader(path, log);
        var reloaded = new TaskCompletionSource<RateCardFile>(TaskCreationOptions.RunContinuationsAsynchronously);
        reloader.Reloaded += (_, card) => reloaded.TrySetResult(card);

        await File.AppendAllTextAsync(path, "ALDER,EUR,3,2.00,4.00\n", Token);
        var card = await reloaded.Task.WaitAsync(TimeSpan.FromSeconds(10), Token);

        Assert.Equal(3, card.PerKgByZone.Count);
    }
}

[CollectionDefinition(Collection, DisableParallelization = true)]
public sealed class WorkingDirectory
{
    /// <summary>The import command changes the process working directory, so these tests never run alongside others.</summary>
    public const string Collection = "working-directory";
}
