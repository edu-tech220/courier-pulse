using CourierPulse.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CourierPulse.Api.Data;

public class CourierPulseDbContext : DbContext
{
    public CourierPulseDbContext(DbContextOptions<CourierPulseDbContext> options)
        : base(options)
    {
    }

    public DbSet<Courier> Couriers => Set<Courier>();

    public DbSet<DeliveryOrder> DeliveryOrders => Set<DeliveryOrder>();

    public DbSet<OrderStatusEvent> OrderStatusEvents => Set<OrderStatusEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Courier>(entity =>
        {
            entity.HasIndex(c => c.Email).IsUnique();
            entity.Property(c => c.Email).HasMaxLength(256).IsRequired();
            entity.Property(c => c.FullName).HasMaxLength(200).IsRequired();
            entity.Property(c => c.Zone).HasMaxLength(100);
            entity.Property(c => c.RowVersion).IsConcurrencyToken();
        });

        modelBuilder.Entity<DeliveryOrder>(entity =>
        {
            entity.HasIndex(o => o.TrackingCode).IsUnique();
            entity.Property(o => o.TrackingCode).HasMaxLength(20).IsRequired();
            entity.Property(o => o.Zone).HasMaxLength(100).IsRequired();
            entity.Property(o => o.PackageWeightKg).HasColumnType("decimal(8,3)");
            entity.Property(o => o.RowVersion).IsConcurrencyToken();

            entity.HasOne(o => o.Courier)
                .WithMany(c => c.AssignedOrders)
                .HasForeignKey(o => o.CourierId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderStatusEvent>(entity =>
        {
            entity.HasOne(e => e.DeliveryOrder)
                .WithMany(o => o.StatusEvents)
                .HasForeignKey(e => e.DeliveryOrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override int SaveChanges()
    {
        StampConcurrencyTokens();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampConcurrencyTokens();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Assigns a fresh application-managed concurrency token to every added or modified entity
    /// that opts in via <see cref="IConcurrencyAware"/>, so SaveChanges fails with
    /// <see cref="DbUpdateConcurrencyException"/> when the row was changed by someone else
    /// since it was loaded — on both SQL Server and SQLite.
    /// </summary>
    private void StampConcurrencyTokens()
    {
        foreach (var entry in ChangeTracker.Entries<IConcurrencyAware>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.RowVersion = Guid.NewGuid().ToByteArray();
            }
        }
    }
}
