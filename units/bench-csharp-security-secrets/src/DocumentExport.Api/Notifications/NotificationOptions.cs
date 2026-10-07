using System.ComponentModel.DataAnnotations;

namespace DocumentExport.Api.Notifications;

/// <summary>The internal notification relay (bound from the "Notifications" section).</summary>
public sealed class NotificationOptions
{
    public const string SectionName = "Notifications";

    [Required]
    public Uri? RelayUrl { get; set; }

    [Required]
    public string Channel { get; set; } = "inventory-exports";
}
