using CourierPulse.Api.Data;
using CourierPulse.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace CourierPulse.Api.Tests.Infrastructure;

/// <summary>
/// Boots the real API pipeline (controllers, JWT auth, rate limiting, ...) against a SQLite
/// in-memory database instead of SQL Server, and seeds it with the same demo accounts
/// (<see cref="DbSeeder"/>) the production app seeds on first run.
/// </summary>
public class CourierPulseWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<CourierPulseDbContext>>();

            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            services.AddDbContext<CourierPulseDbContext>(options => options.UseSqlite(_connection));
        });
    }

    public async Task InitializeAsync()
    {
        // Touching Services spins up the host (and runs ConfigureWebHost) if it hasn't already.
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CourierPulseDbContext>();
        await db.Database.EnsureCreatedAsync();

        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        await DbSeeder.SeedAsync(db, hasher);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection?.Dispose();
        }
    }
}
