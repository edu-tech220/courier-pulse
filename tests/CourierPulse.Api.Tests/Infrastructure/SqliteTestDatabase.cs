using CourierPulse.Api.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CourierPulse.Api.Tests.Infrastructure;

/// <summary>
/// A throwaway SQLite database backed by a single open in-memory connection. Used for service-layer
/// tests that don't need the full HTTP pipeline. <see cref="CreateAdditionalContext"/> opens a second
/// <see cref="CourierPulseDbContext"/> over the *same* connection so tests can simulate a second
/// request racing the one under test, without needing real threads.
/// </summary>
public sealed class SqliteTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public CourierPulseDbContext Context { get; }

    public SqliteTestDatabase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        Context = new CourierPulseDbContext(BuildOptions());
        Context.Database.EnsureCreated();
    }

    public CourierPulseDbContext CreateAdditionalContext() => new(BuildOptions());

    private DbContextOptions<CourierPulseDbContext> BuildOptions() =>
        new DbContextOptionsBuilder<CourierPulseDbContext>()
            .UseSqlite(_connection)
            .Options;

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
