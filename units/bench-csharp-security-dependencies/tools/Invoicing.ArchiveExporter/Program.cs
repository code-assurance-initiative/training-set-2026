using Invoicing.ArchiveExporter;

if (args.Length != 4 || args[0] != "--source" || args[2] != "--output")
{
    Console.Error.WriteLine("usage: invoicing-archive-export --source <archive-directory> --output <file.zip>");
    return 2;
}

var source = args[1];
var output = args[3];
if (!Directory.Exists(source))
{
    Console.Error.WriteLine($"The archive directory '{source}' does not exist.");
    return 2;
}

using var destination = File.Create(output);
var count = new ArchiveExporter(Console.Out).Export(source, destination);
return count > 0 ? 0 : 1;
