using Microsoft.EntityFrameworkCore;

namespace ReportDesk.Api.Reports;

public sealed class ReportsDbContext(DbContextOptions<ReportsDbContext> options) : DbContext(options)
{
    public DbSet<ReportDefinition> Reports => Set<ReportDefinition>();

    public DbSet<ReportSchedule> Schedules => Set<ReportSchedule>();

    public DbSet<Subscriber> Subscribers => Set<Subscriber>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.Entity<ReportDefinition>().ToTable("report_definitions");
        modelBuilder.Entity<ReportSchedule>().ToTable("report_schedules");
        modelBuilder.Entity<Subscriber>().ToTable("report_subscribers");
    }
}
