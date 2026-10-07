using System.ComponentModel.DataAnnotations;

namespace Invoicing.Worker.Processing;

public sealed class WorkerOptions
{
    public const string SectionName = "Worker";

    /// <summary>When the queue is drained, as a five-field cron expression in UTC.</summary>
    [Required]
    public string Schedule { get; set; } = "*/2 * * * *";

    [Range(1, 500)]
    public int BatchSize { get; set; } = 50;
}
