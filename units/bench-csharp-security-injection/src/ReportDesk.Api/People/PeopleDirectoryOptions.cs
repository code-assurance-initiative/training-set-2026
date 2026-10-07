using System.ComponentModel.DataAnnotations;

namespace ReportDesk.Api.People;

/// <summary>The archive's LDAP directory, read with the service's own bind (configured by the platform).</summary>
public sealed class PeopleDirectoryOptions
{
    public const string SectionName = "Directory";

    [Required]
    public string Server { get; set; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; set; } = 636;

    [Required]
    public string BaseDn { get; set; } = string.Empty;

    [Required]
    public string GroupsDn { get; set; } = string.Empty;
}
