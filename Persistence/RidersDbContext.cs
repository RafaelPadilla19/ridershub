using Microsoft.EntityFrameworkCore;
using RidersHub.Domain;

namespace RidersHub.Persistence;

public sealed class RidersDbContext(DbContextOptions<RidersDbContext> options) : DbContext(options)
{
    public DbSet<Rider> Riders => Set<Rider>();
    public DbSet<DeliveryJob> Jobs => Set<DeliveryJob>();
    public DbSet<RiderRating> Ratings => Set<RiderRating>();
    public DbSet<RiderSubscriptionPayment> SubscriptionPayments => Set<RiderSubscriptionPayment>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Rider>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(160).IsRequired();
            e.Property(x => x.Phone).HasMaxLength(20).IsRequired();
            e.Property(x => x.Zone).HasMaxLength(80);
            e.Property(x => x.VehicleType).HasMaxLength(40);
            e.Property(x => x.Plan).HasConversion<string>().HasMaxLength(20);
            e.Ignore(x => x.HasActivePro);
            e.HasIndex(x => x.Phone).IsUnique();
        });

        b.Entity<DeliveryJob>(e =>
        {
            e.Property(x => x.OrderCode).HasMaxLength(40);
            e.Property(x => x.RestaurantName).HasMaxLength(160);
            e.Property(x => x.PickupAddress).HasMaxLength(300);
            e.Property(x => x.DropoffAddress).HasMaxLength(300);
            e.Property(x => x.Zone).HasMaxLength(80);
            e.Property(x => x.Notes).HasMaxLength(400);
            e.Property(x => x.DeliveryFee).HasPrecision(18, 2);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.CallbackUrl).HasMaxLength(400);
            e.Property(x => x.CallbackKey).HasMaxLength(200);
            e.HasIndex(x => new { x.Zone, x.Status });
        });

        b.Entity<RiderRating>(e =>
        {
            e.Property(x => x.Comment).HasMaxLength(400);
            e.HasIndex(x => x.RiderId);
        });

        b.Entity<RiderSubscriptionPayment>(e =>
        {
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.PaymentRef).HasMaxLength(80);
            e.HasIndex(x => x.RiderId);
        });
    }
}
