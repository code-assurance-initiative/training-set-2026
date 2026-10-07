using System.ComponentModel.DataAnnotations;

namespace FleetOps.Infrastructure.Persistence;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    [Required]
    public string ConnectionString { get; set; } = "Data Source=fleetops.db";
}
