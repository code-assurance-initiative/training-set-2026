using System.Reflection;
using ParcelTracking.Cli;

// parcel-admin: operator tool. Output goes to the terminal; the exit code tells scripts what happened.
return args switch
{
    ["check-mappings", var path] => MappingFileCheck.Run(path, Console.Out, Console.Error),
    ["--version"] => PrintVersion(),
    _ => PrintUsage(),
};

static int PrintVersion()
{
    var version = typeof(MappingFileCheck).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
    Console.WriteLine($"parcel-admin {version ?? "unknown"}");
    return 0;
}

static int PrintUsage()
{
    Console.Error.WriteLine("Usage: parcel-admin check-mappings <mapping-file.json>");
    Console.Error.WriteLine("       parcel-admin --version");
    return 64;
}
