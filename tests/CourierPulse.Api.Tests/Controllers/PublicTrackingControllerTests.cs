using System.Net;
using CourierPulse.Api.Dtos;
using CourierPulse.Api.Models;
using CourierPulse.Api.Tests.Infrastructure;

namespace CourierPulse.Api.Tests.Controllers;

public class PublicTrackingControllerTests : IClassFixture<CourierPulseWebApplicationFactory>
{
    private readonly CourierPulseWebApplicationFactory _factory;

    public PublicTrackingControllerTests(CourierPulseWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetByTrackingCode_RequiresNoAuthenticationAndReturnsStatus()
    {
        using var dispatcher = await _factory.CreateClient().AsDispatcherAsync();
        var created = await dispatcher.PostAsJsonAsync("/api/orders", new CreateOrderRequest(
            "123 Pickup St", "456 Dropoff Ave", "ZonePublic", 1m, DateTime.UtcNow, DateTime.UtcNow.AddHours(2), null));
        var order = await created.Content.ReadFromJsonAsync<OrderResponse>();

        using var anonymous = _factory.CreateClient();
        var response = await anonymous.GetAsync($"/api/public/tracking/{order!.TrackingCode}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tracking = await response.Content.ReadFromJsonAsync<PublicTrackingResponse>();
        Assert.Equal(DeliveryOrderStatus.Pending, tracking!.Status);
        Assert.Equal("ZonePublic", tracking.Zone);
    }

    [Fact]
    public async Task GetByTrackingCode_WhenUnknown_ReturnsNotFound()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/public/tracking/CP-DOESNOTEXIST");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetByTrackingCode_DoesNotExposePickupOrDropoffAddresses()
    {
        using var dispatcher = await _factory.CreateClient().AsDispatcherAsync();
        var created = await dispatcher.PostAsJsonAsync("/api/orders", new CreateOrderRequest(
            "Secret Pickup Address", "Secret Dropoff Address", "ZonePrivacy", 1m, DateTime.UtcNow, DateTime.UtcNow.AddHours(2), null));
        var order = await created.Content.ReadFromJsonAsync<OrderResponse>();

        using var anonymous = _factory.CreateClient();
        var response = await anonymous.GetAsync($"/api/public/tracking/{order!.TrackingCode}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("Secret Pickup Address", body);
        Assert.DoesNotContain("Secret Dropoff Address", body);
    }
}
