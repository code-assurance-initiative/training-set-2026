namespace ReportDesk.IntegrationTests;

/// <summary>Shell scripts that stand in for LibreOffice and ImageMagick, so conversions run without either.</summary>
internal static class FakeTools
{
    public const string Soffice = """
        #!/bin/sh
        # Invoked as: soffice --headless --convert-to <format> --outdir <dir> <source>
        format="$3"; outdir="$5"; source="$6"
        name=$(basename "$source"); name="${name%.*}"
        printf 'converted' > "$outdir/$name.$format"
        """;

    public const string Convert = """
        #!/bin/sh
        # Invoked as: convert -thumbnail <size> <input>[0] <output>
        for output; do :; done
        printf 'png' > "$output"
        """;

    public const string Failing = """
        #!/bin/sh
        echo "conversion failed" >&2
        exit 3
        """;

    public static string Write(string directory, string name, string script)
    {
        var path = System.IO.Path.Combine(directory, name);
        File.WriteAllText(path, script.ReplaceLineEndings("\n") + "\n");
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        return path;
    }
}
