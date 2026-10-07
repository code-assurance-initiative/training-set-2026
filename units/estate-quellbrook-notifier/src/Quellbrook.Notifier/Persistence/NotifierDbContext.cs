using Microsoft.EntityFrameworkCore;

namespace Quellbrook.Notifier.Persistence;

public sealed class NotifierDbContext(DbContextOptions<NotifierDbContext> options) : DbContext(options)
{
    public DbSet<Recipient> Recipients => Set<Recipient>();

    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    public DbSet<NotificationLogEntry> NotificationLog => Set<NotificationLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.Entity<Recipient>(recipient =>
        {
            recipient.ToTable("recipients");
            recipient.HasKey(entry => entry.OrderId);
            recipient.Property(entry => entry.Name).HasMaxLength(100);
            recipient.Property(entry => entry.Email).HasMaxLength(254);
            recipient.Property(entry => entry.Phone).HasMaxLength(20);
            recipient.HasIndex(entry => entry.CompletedAt);
        });
        modelBuilder.Entity<ProcessedMessage>(message =>
        {
            message.ToTable("processed_messages");
            message.HasKey(entry => entry.MessageId);
            message.Property(entry => entry.Type).HasMaxLength(100);
            message.HasIndex(entry => entry.ProcessedAt);
        });
        modelBuilder.Entity<NotificationLogEntry>(log =>
        {
            log.ToTable("notification_log");
            log.HasKey(entry => entry.Id);
            log.Property(entry => entry.Kind).HasMaxLength(32);
            log.Property(entry => entry.Channel).HasMaxLength(8);
            log.Property(entry => entry.MaskedRecipient).HasMaxLength(64);
            log.HasIndex(entry => new { entry.OrderId, entry.Kind, entry.Channel }).IsUnique();
            log.HasIndex(entry => entry.SentAt);
        });
    }
}
