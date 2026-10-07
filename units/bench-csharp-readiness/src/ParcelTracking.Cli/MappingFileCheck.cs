using System.Text.Json;
using ParcelTracking.Core.Carriers;
using ParcelTracking.Core.Parcels;

namespace ParcelTracking.Cli;

/// <summary>
/// Checks a carrier status-mapping file before it is proposed for the built-in map: every status must be one the
/// service knows, no two codes may collide once normalised, and codes the built-in map already handles are listed.
/// </summary>
public static class MappingFileCheck
{
    public const int Ok = 0;
    public const int Problems = 1;
    public const int Unreadable = 2;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static int Run(string path, TextWriter output, TextWriter errors)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(errors);

        MappingFile? file;
        try
        {
            file = JsonSerializer.Deserialize<MappingFile>(File.ReadAllText(path), SerializerOptions);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            errors.WriteLine($"Cannot read {path}: {ex.Message}");
            return Unreadable;
        }

        if (file is null || string.IsNullOrWhiteSpace(file.Carrier) || file.Codes is null)
        {
            errors.WriteLine($"{path} must contain a carrier and a codes object.");
            return Unreadable;
        }

        return Check(file, output, errors);
    }

    public static int Check(MappingFile file, TextWriter output, TextWriter errors)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(errors);

        var problems = 0;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (code, status) in file.Codes ?? [])
        {
            var normalised = code.Trim().ToUpperInvariant();
            if (!seen.Add(normalised))
            {
                errors.WriteLine($"{file.Carrier}: code '{code}' collides with another code once normalised to '{normalised}'.");
                problems++;
            }

            if (!Enum.TryParse<ParcelStatus>(status, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
            {
                errors.WriteLine($"{file.Carrier}: code '{code}' maps to unknown status '{status}'.");
                problems++;
            }
            else if (CarrierStatusMap.Default.TryNormalise(file.Carrier, code, out var builtIn))
            {
                var verdict = builtIn == parsed ? "same as built-in" : $"built-in says {builtIn}";
                output.WriteLine($"{file.Carrier}: {normalised} -> {parsed} ({verdict})");
            }
            else
            {
                output.WriteLine($"{file.Carrier}: {normalised} -> {parsed} (new)");
            }
        }

        output.WriteLine($"{seen.Count} codes checked, {problems} problem(s).");
        return problems == 0 ? Ok : Problems;
    }
}

public sealed record MappingFile(string Carrier, Dictionary<string, string>? Codes);
