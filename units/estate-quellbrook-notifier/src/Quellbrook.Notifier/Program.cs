using Quellbrook.Notifier.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder.AddNotifier();
await builder.Build().RunAsync().ConfigureAwait(false);
