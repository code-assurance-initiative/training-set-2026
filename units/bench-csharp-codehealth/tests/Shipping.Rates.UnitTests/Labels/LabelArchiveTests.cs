using Shipping.Rates.Core.Labels;
using Shipping.Rates.UnitTests.TestSupport;

namespace Shipping.Rates.UnitTests.Labels;

public sealed class LabelArchiveTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "labels-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public async Task Saved_labels_can_be_read_back_and_deleted()
    {
        var archive = new LabelArchive(_root, Loggers.For<LabelArchive>());
        var ct = TestContext.Current.CancellationToken;

        await archive.SaveAsync("SR1", "^XA^XZ", ct);

        Assert.True(archive.Exists("SR1"));
        Assert.Equal("^XA^XZ", await archive.ReadAsync("SR1", ct));
        Assert.True(archive.Delete("SR1"));
        Assert.False(archive.Delete("SR1"));
        Assert.Null(await archive.ReadAsync("SR1", ct));
    }

    [Fact]
    public void The_sync_save_writes_the_file()
    {
        var archive = new LabelArchive(_root, Loggers.For<LabelArchive>());

        archive.Save("SR2", "^XA^XZ");

        Assert.True(File.Exists(Path.Combine(_root, "SR2.zpl")));
    }

    [Theory]
    [InlineData("../escape")]
    [InlineData("a/b")]
    public void Label_ids_cannot_leave_the_archive(string labelId)
    {
        var archive = new LabelArchive(_root, Loggers.For<LabelArchive>());

        Assert.Throws<ArgumentException>(() => archive.PathFor(labelId));
    }
}
