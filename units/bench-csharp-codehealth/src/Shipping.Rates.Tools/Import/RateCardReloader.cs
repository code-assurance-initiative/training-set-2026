namespace Shipping.Rates.Tools.Import;

/// <summary>Re-reads a rate-card CSV whenever it changes on disk and announces the new card.</summary>
public sealed class RateCardReloader : IDisposable
{
    private readonly FileSystemWatcher _watcher;
    private readonly TextWriter _log;

    public RateCardReloader(string path, TextWriter log)
    {
        _log = log;
        _watcher = new FileSystemWatcher(Path.GetDirectoryName(path), Path.GetFileName(path));
        _watcher.Changed += OnChanged;
        _watcher.EnableRaisingEvents = true;
    }

    public event EventHandler<RateCardFile> Reloaded;

    private async void OnChanged(object sender, FileSystemEventArgs e)
    {
        try
        {
            var text = await File.ReadAllTextAsync(e.FullPath);
            Reloaded?.Invoke(this, RateCardCsv.Parse(text));
        }
        catch (FileNotFoundException)
        {
            // The file was replaced again before we could read it; the Changed event raised for the
            // replacement reloads it, so there is nothing to do for this one.
        }
        catch (Exception ex)
        {
            await _log.WriteLineAsync($"Reloading {e.FullPath} failed: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _watcher.Changed -= OnChanged;
        _watcher.Dispose();
    }
}
