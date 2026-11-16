using CourierPulse.Api.Models;
using CourierPulse.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace CourierPulse.Api.Data;

/// <summary>
/// Seeds a demo admin, dispatcher and a couple of available couriers so the API and frontend
/// are usable out of the box. Credentials are documented in the README — this is sample data
/// for a portfolio project, not a production seeding strategy.
/// </summary>
public static class DbSeeder
{
    public const string AdminEmail = "admin@courierpulse.dev";
    public const string AdminPassword = "Admin#12345";

    public static async Task SeedAsync(CourierPulseDbContext db, IPasswordHasher passwordHasher)
    {
        if (await db.Couriers.AnyAsync())
        {
            return;
        }

        var now = DateTime.UtcNow;

        db.Couriers.AddRange(
            new Courier
            {
                FullName = "Demo Admin",
                Email = AdminEmail,
                PasswordHash = passwordHasher.HashPassword(AdminPassword),
                Role = CourierRole.Admin,
                Zone = string.Empty,
                Availability = CourierAvailability.Offline,
                CreatedAt = now,
            },
            new Courier
            {
                FullName = "Demo Dispatcher",
                Email = "dispatcher@courierpulse.dev",
                PasswordHash = passwordHasher.HashPassword("Dispatcher#12345"),
                Role = CourierRole.Dispatcher,
                Zone = string.Empty,
                Availability = CourierAvailability.Offline,
                CreatedAt = now,
            },
            new Courier
            {
                FullName = "Alex Downtown",
                Email = "courier.downtown@courierpulse.dev",
                PasswordHash = passwordHasher.HashPassword("Courier#12345"),
                Role = CourierRole.Courier,
                Zone = "Downtown",
                Availability = CourierAvailability.Available,
                CreatedAt = now,
            },
            new Courier
            {
                FullName = "Jordan North",
                Email = "courier.north@courierpulse.dev",
                PasswordHash = passwordHasher.HashPassword("Courier#12345"),
                Role = CourierRole.Courier,
                Zone = "North",
                Availability = CourierAvailability.Available,
                CreatedAt = now,
            });

        await db.SaveChangesAsync();
    }
}
