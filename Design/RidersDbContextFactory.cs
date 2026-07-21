using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using RidersHub.Persistence;

namespace RidersHub.Design;

/// <summary>Fábrica usada solo por `dotnet ef` para crear migraciones sin arrancar el host completo.</summary>
public sealed class RidersDbContextFactory : IDesignTimeDbContextFactory<RidersDbContext>
{
    public RidersDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Riders")
            ?? "Host=localhost;Port=5432;Database=riders;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<RidersDbContext>().UseNpgsql(connectionString).Options;
        return new RidersDbContext(options);
    }
}
