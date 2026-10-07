namespace Shipping.Rates.Tools;

public static class Program
{
    public static int Main(string[] args)
    {
        using var cancel = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cancel.Cancel();
        };
        return CommandLine.RunAsync(args, Console.Out, Console.Error, cancel.Token).GetAwaiter().GetResult();
    }
}
