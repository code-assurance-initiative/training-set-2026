using Microsoft.EntityFrameworkCore;
using ParcelTracking.Core.Notifications;
using ParcelTracking.Core.Parcels;

namespace ParcelTracking.Infrastructure.Persistence;

public sealed class TrackingDbContext(DbContextOptions<TrackingDbContext> options) : DbContext(options)
{
    public DbSet<Parcel> Parcels => Set<Parcel>();

    public DbSet<TrackingEvent> TrackingEvents => Set<TrackingEvent>();

    public DbSet<PendingNotification> PendingNotifications => Set<PendingNotification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Parcel>(parcel =>
        {
            parcel.ToTable("parcels");
            parcel.HasKey(p => p.Id);
            parcel.Property(p => p.Id).ValueGeneratedNever();
            parcel.Property(p => p.TrackingNumber).HasMaxLength(40).IsRequired();
            parcel.HasIndex(p => p.TrackingNumber).IsUnique();
            parcel.Property(p => p.MerchantId).HasMaxLength(64).IsRequired();
            parcel.HasIndex(p => new { p.MerchantId, p.RegisteredAt });
            parcel.Property(p => p.CarrierCode).HasMaxLength(16).IsRequired();
            parcel.Property(p => p.DestinationPostalCode).HasMaxLength(12).IsRequired();
            parcel.Property(p => p.Status).HasConversion<string>().HasMaxLength(24);
            parcel.HasIndex(p => new { p.Status, p.LastPolledAt });
            parcel.Property(p => p.PickupPointId).HasMaxLength(32);
            parcel.Property(p => p.RedirectNote).HasMaxLength(500);
            parcel.HasMany(p => p.Events).WithOne().HasForeignKey(e => e.ParcelId).OnDelete(DeleteBehavior.Cascade);
            parcel.Navigation(p => p.Events).HasField("_events").UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<TrackingEvent>(trackingEvent =>
        {
            trackingEvent.ToTable("tracking_events");
            trackingEvent.HasKey(e => e.Id);
            trackingEvent.Property(e => e.Id).ValueGeneratedNever();
            trackingEvent.Property(e => e.CarrierStatusCode).HasMaxLength(32).IsRequired();
            trackingEvent.Property(e => e.Status).HasConversion<string>().HasMaxLength(24);
            trackingEvent.Property(e => e.Location).HasMaxLength(120);
        });

        modelBuilder.Entity<PendingNotification>(notification =>
        {
            notification.ToTable("pending_notifications");
            notification.HasKey(n => n.Id);
            notification.Property(n => n.Id).ValueGeneratedNever();
            notification.Property(n => n.MerchantId).HasMaxLength(64).IsRequired();
            notification.Property(n => n.TrackingNumber).HasMaxLength(40).IsRequired();
            notification.Property(n => n.Status).HasConversion<string>().HasMaxLength(24);
            notification.HasIndex(n => new { n.DeliveredAt, n.NextAttemptAt });
        });
    }
}
