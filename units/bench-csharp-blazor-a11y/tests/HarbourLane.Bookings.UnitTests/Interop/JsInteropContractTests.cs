using System.Text.RegularExpressions;
using HarbourLane.Bookings.UnitTests.TestSupport;

namespace HarbourLane.Bookings.UnitTests.Interop;

/// <summary>
/// The typed interop wrappers in <c>Interop/</c> call JavaScript by name. These tests fail when a name they call is
/// renamed or removed in <c>wwwroot/js</c>, or a module they import is moved.
/// </summary>
public sealed partial class JsInteropContractTests
{
    private static readonly string[] PlatformObjects = ["navigator.", "window.", "document."];

    public static TheoryData<string, string> WrapperBindings()
    {
        var data = new TheoryData<string, string>();
        foreach (var file in Directory.EnumerateFiles(Path.Combine(RepositoryFiles.WebProject, "Interop"), "*.cs"))
        {
            foreach (Match call in InvocationName().Matches(File.ReadAllText(file)))
            {
                var name = call.Groups["name"].Value;
                if (name != "import" && !PlatformObjects.Any(p => name.StartsWith(p, StringComparison.Ordinal)))
                {
                    data.Add(Path.GetFileName(file), name);
                }
            }
        }

        return data;
    }

    [Fact]
    public void The_wrappers_call_at_least_one_script_function()
    {
        Assert.NotEmpty(WrapperBindings());
    }

    [Theory]
    [MemberData(nameof(WrapperBindings))]
    public void Every_function_a_wrapper_calls_is_defined_in_the_scripts(string wrapper, string name)
    {
        var scripts = ScriptsText();
        var dot = name.LastIndexOf('.');
        var function = dot < 0 ? name : name[(dot + 1)..];

        if (dot >= 0)
        {
            var owner = name[..dot];
            Assert.True(
                scripts.Contains($"window.{owner} =", StringComparison.Ordinal),
                $"{wrapper} calls {name}, but no script assigns window.{owner}.");
        }

        var definition = new Regex(
            $@"(?:\bfunction\s+{Regex.Escape(function)}\s*\(|^\s*{Regex.Escape(function)}\s*\([^)]*\)\s*\{{)",
            RegexOptions.Multiline,
            TimeSpan.FromSeconds(1));
        Assert.True(definition.IsMatch(scripts), $"{wrapper} calls {name}, but no script defines {function}.");
    }

    [Fact]
    public void Every_imported_module_exists()
    {
        foreach (var file in Directory.EnumerateFiles(Path.Combine(RepositoryFiles.WebProject, "Interop"), "*.cs"))
        {
            foreach (Match import in ModuleImport().Matches(File.ReadAllText(file)))
            {
                var relative = import.Groups["path"].Value.TrimStart('.', '/');
                Assert.True(
                    File.Exists(Path.Combine(RepositoryFiles.WebProject, "wwwroot", relative)),
                    $"{Path.GetFileName(file)} imports {import.Groups["path"].Value}, which is not in wwwroot.");
            }
        }
    }

    private static string ScriptsText() =>
        string.Join('\n', Directory.EnumerateFiles(Path.Combine(RepositoryFiles.WebProject, "wwwroot", "js"), "*.js").Select(File.ReadAllText));

    [GeneratedRegex(@"Invoke(?:Void)?Async(?:<[^>]+>)?\(\s*""(?<name>[^""]+)""")]
    private static partial Regex InvocationName();

    [GeneratedRegex(@"""import""\s*,\s*(?:\w+\s*,\s*)?""(?<path>[^""]+)""")]
    private static partial Regex ModuleImport();
}
