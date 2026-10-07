using System.Text.Json;

namespace Shipping.Rates.Tools.Import;

/// <summary>Converts the carriers' CSV rate cards into the JSON documents the rating service downloads.</summary>
public sealed class RateCardImportCommand
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly TextWriter _output;

    public RateCardImportCommand(TextWriter output)
    {
        _output = output;
    }

    public int Run(string directory)
    {
        var previous = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(directory);
        Directory.CreateDirectory("out");
        var imported = 0;
        foreach (var file in Directory.GetFiles(".", "*.csv"))
        {
            var card = RateCardCsv.Parse(File.ReadAllText(file));
            File.WriteAllText(Path.Combine("out", card.Carrier.ToLowerInvariant() + ".json"), JsonSerializer.Serialize(card, Json));
            imported++;
        }

        Directory.SetCurrentDirectory(previous);
        _output.WriteLine($"Imported {imported} rate card(s) from {directory}");
        return 0;
    }

    public int Validate(string directory)
    {
        var previous = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(directory);
        try
        {
            var invalid = 0;
            foreach (var file in Directory.GetFiles(".", "*.csv"))
            {
                try
                {
                    _output.WriteLine(RateCardCsv.Parse(File.ReadAllText(file)).Describe());
                }
                catch (FormatException ex)
                {
                    _output.WriteLine($"{Path.GetFileName(file)}: {ex.Message}");
                    invalid++;
                }
            }

            return invalid == 0 ? 0 : 1;
        }
        finally
        {
            Directory.SetCurrentDirectory(previous);
        }
    }
}
