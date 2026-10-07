using System.Text;
using ReportDesk.Api.Importing;

namespace ReportDesk.UnitTests.Importing;

public sealed class BundleTests
{
    [Fact]
    public void ReadsTheEntriesOfABundle()
    {
        using var bundle = Write(("minutes.pdf", "PDF"u8.ToArray()), ("notes.txt", "hello"u8.ToArray()));
        var entries = BundleReader.ReadEntries(bundle);

        Assert.Equal(["minutes.pdf", "notes.txt"], entries.Select(entry => entry.Name));
        Assert.Equal("hello"u8.ToArray(), entries[1].Content);
    }

    [Fact]
    public void RejectsAFileWithoutTheBundleMagic()
    {
        using var stream = new MemoryStream("not a bundle"u8.ToArray());
        Assert.Throws<InvalidDataException>(() => BundleReader.ReadEntries(stream));
    }

    [Fact]
    public void RejectsAnIndexNameLongerThanTheLimit()
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(BundleIndexReader.Magic);
            writer.Write((ushort)1);
            writer.Write(BundleIndexReader.MaxNameBytes + 1);
        }

        stream.Position = 0;
        Assert.Throws<InvalidDataException>(() => BundleReader.ReadEntries(stream));
    }

    private static MemoryStream Write(params (string Name, byte[] Content)[] entries)
    {
        var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(BundleIndexReader.Magic);
            writer.Write((ushort)entries.Length);
            foreach (var (name, _) in entries)
            {
                var bytes = Encoding.UTF8.GetBytes(name);
                writer.Write(bytes.Length);
                writer.Write(bytes);
            }

            foreach (var (_, content) in entries)
            {
                writer.Write(content.Length);
                writer.Write(content);
            }
        }

        stream.Position = 0;
        return stream;
    }
}
