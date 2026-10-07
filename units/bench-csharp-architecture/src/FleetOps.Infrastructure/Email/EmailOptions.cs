using System.ComponentModel.DataAnnotations;

namespace FleetOps.Infrastructure.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>The relay picks messages up from this directory.</summary>
    [Required]
    public string PickupDirectory { get; set; } = "mail-pickup";

    [Required]
    [EmailAddress]
    public string From { get; set; } = string.Empty;
}
