using System.Net;
using CourierPulse.Api.Dtos;
using CourierPulse.Api.Models;
using CourierPulse.Api.Tests.Infrastructure;

namespace CourierPulse.Api.Tests.Controllers;

public class OrdersControllerTests : IClassFixture<CourierPulseWebApplicationFactory>
{
    private readonly CourierPulseWebApplicationFactory _factory;

    public OrdersControllerTests(CourierPulseWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_AsDispatcher_ReturnsCreatedOrderWithPendingStatus()
    {
        using var client = await _factory.CreateClient().AsDispatcherAsync();

        var response = await client.PostAsJsonAsync("/api/orders", NewOrderRequest("ZoneCreate"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.Equal(DeliveryOrderStatus.Pending, order!.Status);
        Assert.StartsWith("CP-", order.TrackingCode);
    }

    [Fact]
    public async Task Create_AsCourier_ReturnsForbidden()
    {
        using var client = await _factory.CreateClient().AsDowntownCourierAsync();

        var response = await client.PostAsJsonAsync("/api/orders", NewOrderRequest("Downtown"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithWindowEndBeforeStart_ReturnsValidationProblem()
    {
        using var client = await _factory.CreateClient().AsDispatcherAsync();
        var request = NewOrderRequest("ZoneCreate") with
        {
            PromisedWindowStart = DateTime.UtcNow.AddHours(2),
            PromisedWindowEnd = DateTime.UtcNow,
        };

        var response = await client.PostAsJsonAsync("/api/orders", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_WithoutAuthentication_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/orders");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_FilteredByZone_ReturnsOnlyOrdersInThatZoneWithAccurateTotalCount()
    {
        using var dispatcher = await _factory.CreateClient().AsDispatcherAsync();
        const string zone = "ZoneList";

        for (var i = 0; i < 3; i++)
        {
            var created = await dispatcher.PostAsJsonAsync("/api/orders", NewOrderRequest(zone));
            created.EnsureSuccessStatusCode();
        }

        var response = await dispatcher.GetAsync($"/api/orders?zone={zone}&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<OrderResponse>>();
        Assert.Equal(3, page!.TotalCount);
        Assert.All(page.Items, o => Assert.Equal(zone, o.Zone));
    }

    [Fact]
    public async Task GetMine_AsCourier_ReturnsOnlyOrdersAssignedToThatCourier()
    {
        using var dispatcher = await _factory.CreateClient().AsDispatcherAsync();
        var order = await CreateOrderAsync(dispatcher, "Downtown");
        await dispatcher.PostAsync($"/api/orders/{order.Id}/assign", content: null);

        using var courier = await _factory.CreateClient().AsDowntownCourierAsync();
        var response = await courier.GetAsync("/api/orders/mine");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<OrderResponse>>();
        Assert.Contains(page!.Items, o => o.Id == order.Id);
        Assert.All(page.Items, o => Assert.NotNull(o.CourierId));
    }

    [Fact]
    public async Task GetMine_AsDispatcher_ReturnsForbidden()
    {
        using var dispatcher = await _factory.CreateClient().AsDispatcherAsync();

        var response = await dispatcher.GetAsync("/api/orders/mine");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetById_AsNonOwningCourier_ReturnsForbidden()
    {
        using var dispatcher = await _factory.CreateClient().AsDispatcherAsync();
        var created = await CreateOrderAsync(dispatcher, "ZoneForbidden");

        using var courier = await _factory.CreateClient().AsNorthCourierAsync();
        var response = await courier.GetAsync($"/api/orders/{created.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetById_WhenOrderDoesNotExist_ReturnsNotFound()
    {
        using var dispatcher = await _factory.CreateClient().AsDispatcherAsync();

        var response = await dispatcher.GetAsync("/api/orders/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Assign_WhenNoCourierAvailableInZone_ReturnsUnprocessableEntity()
    {
        using var dispatcher = await _factory.CreateClient().AsDispatcherAsync();
        var created = await CreateOrderAsync(dispatcher, "ZoneWithNoCouriers");

        var response = await dispatcher.PostAsync($"/api/orders/{created.Id}/assign", content: null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task FullLifecycle_AssignPickupTransitDeliver_SucceedsAndDeliverIsIdempotent()
    {
        using var dispatcher = await _factory.CreateClient().AsDispatcherAsync();
        var created = await CreateOrderAsync(dispatcher, "Downtown");

        var assignResponse = await dispatcher.PostAsync($"/api/orders/{created.Id}/assign", content: null);
        Assert.Equal(HttpStatusCode.OK, assignResponse.StatusCode);
        var assigned = await assignResponse.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.Equal(DeliveryOrderStatus.Assigned, assigned!.Status);

        using var courier = await _factory.CreateClient().AsDowntownCourierAsync();

        var pickedUp = await courier.PostAsJsonAsync($"/api/orders/{created.Id}/transition", new TransitionRequest(DeliveryOrderStatus.PickedUp));
        Assert.Equal(HttpStatusCode.OK, pickedUp.StatusCode);

        var inTransit = await courier.PostAsJsonAsync($"/api/orders/{created.Id}/transition", new TransitionRequest(DeliveryOrderStatus.InTransit));
        Assert.Equal(HttpStatusCode.OK, inTransit.StatusCode);

        courier.DefaultRequestHeaders.Add("Idempotency-Key", "lifecycle-test-key");
        var firstDeliver = await courier.PutAsync($"/api/orders/{created.Id}/deliver", content: null);
        Assert.Equal(HttpStatusCode.OK, firstDeliver.StatusCode);

        var secondDeliver = await courier.PutAsync($"/api/orders/{created.Id}/deliver", content: null);
        Assert.Equal(HttpStatusCode.OK, secondDeliver.StatusCode);

        var timelineResponse = await dispatcher.GetAsync($"/api/orders/{created.Id}/timeline");
        var timeline = await timelineResponse.Content.ReadFromJsonAsync<List<OrderStatusEventResponse>>();
        Assert.Single(timeline!, e => e.ToStatus == DeliveryOrderStatus.Delivered);
    }

    [Fact]
    public async Task Transition_SkippingAStep_ReturnsConflict()
    {
        using var dispatcher = await _factory.CreateClient().AsDispatcherAsync();
        var created = await CreateOrderAsync(dispatcher, "Downtown");
        await dispatcher.PostAsync($"/api/orders/{created.Id}/assign", content: null);

        using var courier = await _factory.CreateClient().AsDowntownCourierAsync();
        var response = await courier.PostAsJsonAsync($"/api/orders/{created.Id}/transition", new TransitionRequest(DeliveryOrderStatus.Delivered));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Deliver_WithoutIdempotencyKeyHeader_ReturnsBadRequest()
    {
        using var dispatcher = await _factory.CreateClient().AsDispatcherAsync();
        var created = await CreateOrderAsync(dispatcher, "Downtown");
        await dispatcher.PostAsync($"/api/orders/{created.Id}/assign", content: null);

        using var courier = await _factory.CreateClient().AsDowntownCourierAsync();
        await courier.PostAsJsonAsync($"/api/orders/{created.Id}/transition", new TransitionRequest(DeliveryOrderStatus.PickedUp));
        await courier.PostAsJsonAsync($"/api/orders/{created.Id}/transition", new TransitionRequest(DeliveryOrderStatus.InTransit));

        var response = await courier.PutAsync($"/api/orders/{created.Id}/deliver", content: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<OrderResponse> CreateOrderAsync(HttpClient dispatcherClient, string zone)
    {
        var response = await dispatcherClient.PostAsJsonAsync("/api/orders", NewOrderRequest(zone));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OrderResponse>())!;
    }

    private static CreateOrderRequest NewOrderRequest(string zone) => new(
        PickupAddress: "123 Pickup St",
        DropoffAddress: "456 Dropoff Ave",
        Zone: zone,
        PackageWeightKg: 1.5m,
        PromisedWindowStart: DateTime.UtcNow,
        PromisedWindowEnd: DateTime.UtcNow.AddHours(3),
        Notes: null);
}
