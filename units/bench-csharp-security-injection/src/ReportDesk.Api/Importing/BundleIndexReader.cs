using System.Text;

namespace ReportDesk.Api.Importing;

/// <summary>
/// Reads the index at the start of a bundle: a magic number, an entry count and the entry names, each as a
/// 32-bit length followed by that many bytes of UTF-8.
/// </summary>
public static class BundleIndexReader
{
    public const uint Magic = 0x31424452; // "RDB1"
    public const int MaxEntries = 500;
    public const int MaxNameBytes = 1024;

    public static IReadOnlyList<string> ReadIndex(BinaryReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        if (reader.ReadUInt32() != Magic)
        {
            throw new InvalidDataException("Not a bundle file.");
        }

        var count = reader.ReadUInt16();
        if (count > MaxEntries)
        {
            throw new InvalidDataException($"A bundle may carry at most {MaxEntries} entries.");
        }

        var names = new List<string>(count);
        for (var i = 0; i < count; i++)
        {
            var nameLength = reader.ReadInt32();
            if (nameLength <= 0 || nameLength > MaxNameBytes)
            {
                throw new InvalidDataException("Bundle entry name has an invalid length.");
            }

            names.Add(Encoding.UTF8.GetString(reader.ReadBytes(nameLength)));
        }

        return names;
    }
}
