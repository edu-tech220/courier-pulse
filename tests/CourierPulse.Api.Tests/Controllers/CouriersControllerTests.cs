using System.Net;
using System.Net.Http.Json;
using CourierPulse.Api.Dtos;
using CourierPulse.Api.Models;
using CourierPulse.Api.Tests.Infrastructure;

namespace CourierPulse.Api.Tests.Controllers;

public class CouriersControllerTests : IClassFixture<CourierPulseWebApplicationFactory>
{
    private readonly CourierPulseWebApplicationFactory _factory;

    public CouriersControllerTests(CourierPulseWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_AsAdmin_CreatesTheCourierAccount()
    {
        using var admin = await _factory.CreateClient().AsAdminAsync();
        var email = $"new-{Guid.NewGuid():N}@courierpulse.dev";

        var response = await admin.PostAsJsonAsync("/api/couriers", new CreateCourierRequest("New Courier", email, "Password#123", CourierRole.Courier, "East"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CourierResponse>();
        Assert.Equal(email, created!.Email);
        Assert.Equal(CourierAvailability.Offline, created.Availability);
    }

    [Fact]
    public async Task Create_WithDuplicateEmail_ReturnsConflict()
    {
        using var admin = await _factory.CreateClient().AsAdminAsync();
        var email = $"dup-{Guid.NewGuid():N}@courierpulse.dev";
        var request = new CreateCourierRequest("Courier", email, "Password#123", CourierRole.Courier, "East");

        var first = await admin.PostAsJsonAsync("/api/couriers", request);
        first.EnsureSuccessStatusCode();

        var second = await admin.PostAsJsonAsync("/api/couriers", request);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Create_AsDispatcher_ReturnsForbidden()
    {
        using var dispatcher = await _factory.CreateClient().AsDispatcherAsync();

        var response = await dispatcher.PostAsJsonAsync(
            "/api/couriers",
            new CreateCourierRequest("Courier", $"nope-{Guid.NewGuid():N}@courierpulse.dev", "Password#123", CourierRole.Courier, "East"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMyAvailability_AsCourier_UpdatesOwnStatus()
    {
        using var courier = await _factory.CreateClient().AsNorthCourierAsync();

        var response = await courier.PatchAsJsonAsync("/api/couriers/me/availability", new UpdateAvailabilityRequest(CourierAvailability.Busy));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<CourierResponse>();
        Assert.Equal(CourierAvailability.Busy, updated!.Availability);

        // Restore so other tests in this fixture can still find an available North courier.
        await courier.PatchAsJsonAsync("/api/couriers/me/availability", new UpdateAvailabilityRequest(CourierAvailability.Available));
    }

    [Fact]
    public async Task GetAll_AsCourier_ReturnsForbidden()
    {
        using var courier = await _factory.CreateClient().AsDowntownCourierAsync();

        var response = await courier.GetAsync("/api/couriers");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
