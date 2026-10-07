using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ReportDesk.Api.Conversion;

namespace ReportDesk.UnitTests.Conversion;

public sealed class ConversionTests : IDisposable
{
    private readonly TempDirectory _temp = new();

    private (DocumentConverter Converter, ThumbnailRenderer Thumbnails, string Source) Create(string soffice, string convert)
    {
        var storage = TestOptions.Storage(_temp.Path);
        Directory.CreateDirectory(storage.Value.ScratchRoot);
        var conversion = Options.Create(new ConversionOptions
        {
            SofficePath = FakeTools.Write(_temp.Path, "soffice", soffice),
            ConvertPath = FakeTools.Write(_temp.Path, "convert", convert),
        });
        var source = Path.Combine(storage.Value.ScratchRoot, "monthly.html");
        File.WriteAllText(source, "<h1>Monthly</h1>");
        return (new DocumentConverter(conversion, storage, NullLogger<DocumentConverter>.Instance), new ThumbnailRenderer(conversion), source);
    }

    [Fact]
    public async Task ConvertsAndRendersAThumbnail()
    {
        var (converter, thumbnails, source) = Create(FakeTools.Soffice, FakeTools.Convert);

        var output = await converter.ConvertAsync(source, "pdf", TestContext.Current.CancellationToken);
        var thumbnail = await thumbnails.RenderAsync(output, ThumbnailSize.Small, TestContext.Current.CancellationToken);

        Assert.Equal("monthly.pdf", Path.GetFileName(output));
        Assert.Equal("converted", await File.ReadAllTextAsync(output, TestContext.Current.CancellationToken));
        Assert.Equal("png", await File.ReadAllTextAsync(thumbnail, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ReportsAFailedConversion()
    {
        var (converter, _, source) = Create(FakeTools.Failing, FakeTools.Convert);
        await Assert.ThrowsAsync<ConversionFailedException>(() => converter.ConvertAsync(source, "pdf", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ReportsAFailedThumbnail()
    {
        var (_, thumbnails, source) = Create(FakeTools.Soffice, FakeTools.Failing);
        await Assert.ThrowsAsync<ConversionFailedException>(() => thumbnails.RenderAsync(source, ThumbnailSize.Large, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RefusesAnUndefinedThumbnailSize()
    {
        var (_, thumbnails, source) = Create(FakeTools.Soffice, FakeTools.Convert);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => thumbnails.RenderAsync(source, (ThumbnailSize)300, TestContext.Current.CancellationToken));
    }

    public void Dispose() => _temp.Dispose();
}
