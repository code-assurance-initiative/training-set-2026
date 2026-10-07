namespace HarbourLane.Bookings.UnitTests.TestSupport;

internal static class RepositoryFiles
{
    private static readonly Lazy<string> RootPath = new(FindRoot);

    public static string Root => RootPath.Value;

    public static string WebProject => Path.Combine(Root, "src", "HarbourLane.Bookings.Web");

    private static string FindRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "HarbourLane.Bookings.slnx")))
            {
                return dir.FullName;
            }
        }

        throw new InvalidOperationException("The repository root (HarbourLane.Bookings.slnx) was not found above the test binaries.");
    }
}
