using System.Text;

namespace ReportDesk.Api.Importing;

/// <summary>Reads a bundle: the index, then each entry's content as a 32-bit length followed by the bytes.</summary>
public static class BundleReader
{
    public static IReadOnlyList<BundleEntry> ReadEntries(Stream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
        var names = BundleIndexReader.ReadIndex(reader);
        var entries = new List<BundleEntry>(names.Count);
        foreach (var name in names)
        {
            var length = reader.ReadInt32();
            var content = reader.ReadBytes(length);
            entries.Add(new BundleEntry(name, content));
        }

        return entries;
    }
}
