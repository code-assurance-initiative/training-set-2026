using System.ComponentModel.DataAnnotations;

namespace ReportDesk.Api.Hosting;

/// <summary>Where attachments, report templates and conversion scratch files live on the host.</summary>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    [Required]
    public string AttachmentsRoot { get; set; } = string.Empty;

    [Required]
    public string TemplatesRoot { get; set; } = string.Empty;

    [Required]
    public string ScratchRoot { get; set; } = string.Empty;
}
