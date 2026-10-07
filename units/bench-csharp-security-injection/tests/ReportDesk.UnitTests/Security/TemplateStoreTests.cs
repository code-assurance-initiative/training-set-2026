using ReportDesk.Api.Templates;

namespace ReportDesk.UnitTests.Security;

public sealed class TemplateStoreTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly TemplateStore _store;

    public TemplateStoreTests()
    {
        var options = TestOptions.Storage(_temp.Path);
        Directory.CreateDirectory(options.Value.TemplatesRoot);
        File.WriteAllText(Path.Combine(options.Value.TemplatesRoot, "monthly.html"), "<h1>Monthly</h1>");
        File.WriteAllText(Path.Combine(_temp.Path, "outside.html"), "secret");
        _store = new TemplateStore(options);
    }

    [Fact]
    public async Task ReadsATemplateByName()
    {
        Assert.Equal("<h1>Monthly</h1>", await _store.ReadAsync("monthly", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ReturnsNullForAnUnknownTemplate()
    {
        Assert.Null(await _store.ReadAsync("weekly", TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("../outside")]
    [InlineData("sub/../../outside")]
    public async Task RefusesNamesThatLeaveTheTemplateDirectory(string name)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _store.ReadAsync(name, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RefusesAnAbsolutePath()
    {
        var absolute = Path.Combine(_temp.Path, "outside");
        await Assert.ThrowsAsync<ArgumentException>(() => _store.ReadAsync(absolute, TestContext.Current.CancellationToken));
    }

    public void Dispose() => _temp.Dispose();
}
