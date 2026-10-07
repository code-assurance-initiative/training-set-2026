using Invoicing.Rendering;
using Invoicing.Worker.Jobs;
using Invoicing.Worker.Mail;
using Invoicing.Worker.Processing;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInvoiceRendering();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddOptions<WorkerOptions>().BindConfiguration(WorkerOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<SmtpOptions>().BindConfiguration(SmtpOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();

builder.Services.AddSingleton<IRenderJobStore>(provider => new MySqlRenderJobStore(
    builder.Configuration.GetConnectionString("Billing")
        ?? throw new InvalidOperationException("ConnectionStrings:Billing is not configured."),
    $"{Environment.MachineName}-{Environment.ProcessId}",
    provider.GetRequiredService<ILogger<MySqlRenderJobStore>>()));
builder.Services.AddSingleton<IInvoiceMailer, SmtpInvoiceMailer>();
builder.Services.AddSingleton<RenderQueueProcessor>();
builder.Services.AddHostedService<RenderQueueWorker>();

builder.Build().Run();
