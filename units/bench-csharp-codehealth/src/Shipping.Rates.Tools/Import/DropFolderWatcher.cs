namespace Shipping.Rates.Tools.Import;

/// <summary>
/// Waits for the carriers' upload job to drop rate-card files into a folder. Files already there when the wait
/// starts count too, so a drop that lands just before the command runs is not missed.
/// </summary>
public sealed class DropFolderWatcher
{
    public IReadOnlyList<string> WaitForFiles(string directory, int expected, TimeSpan timeout)
    {
        var arrived = new List<string>(Directory.GetFiles(directory, "*.csv"));
        using var signal = new AutoResetEvent(false);
        using var watcher = new FileSystemWatcher(directory, "*.csv");
        watcher.Created += (sender, e) =>
        {
            arrived.Add(e.FullPath);
            signal.Set();
        };
        watcher.EnableRaisingEvents = true;

        var deadline = DateTime.UtcNow + timeout;
        while (arrived.Count < expected)
        {
            var remaining = deadline - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero || !signal.WaitOne(remaining))
            {
                break;
            }
        }

        return arrived.ToList();
    }
}
