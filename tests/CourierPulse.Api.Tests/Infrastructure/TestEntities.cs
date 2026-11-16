using CourierPulse.Api.Models;

namespace CourierPulse.Api.Tests.Infrastructure;

public static class TestEntities
{
    public static Courier NewCourier(
        string zone = "Downtown",
        CourierAvailability availability = CourierAvailability.Available,
        CourierRole role = CourierRole.Courier,
        string? email = null) => new()
    {
        FullName = "Test Courier",
        Email = email ?? $"courier-{Guid.NewGuid():N}@test.local",
        PasswordHash = "unused-in-these-tests",
        Role = role,
        Zone = zone,
        Availability = availability,
        CreatedAt = DateTime.UtcNow,
    };

    public static DeliveryOrder NewPendingOrder(string zone = "Downtown", DateTime? promisedWindowEnd = null) => new()
    {
        TrackingCode = $"CP-{Guid.NewGuid():N}".ToUpperInvariant()[..10],
        PickupAddress = "123 Pickup St",
        DropoffAddress = "456 Dropoff Ave",
        Zone = zone,
        PackageWeightKg = 2.5m,
        PromisedWindowStart = DateTime.UtcNow,
        PromisedWindowEnd = promisedWindowEnd ?? DateTime.UtcNow.AddHours(2),
        Status = DeliveryOrderStatus.Pending,
        CreatedAt = DateTime.UtcNow,
    };
}
