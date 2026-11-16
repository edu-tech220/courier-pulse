using CourierPulse.Api.Models;
using CourierPulse.Api.Services;
using CourierPulse.Api.Tests.Infrastructure;

namespace CourierPulse.Api.Tests.Services;

public class OrderTransitionServiceTests
{
    [Fact]
    public async Task TransitionAsync_WithALegalStep_UpdatesStatus()
    {
        using var testDb = new SqliteTestDatabase();
        var (order, courierId) = await SeedAssignedOrderAsync(testDb);

        var sut = new OrderTransitionService(testDb.Context, TimeProvider.System);
        var result = await sut.TransitionAsync(order.Id, DeliveryOrderStatus.PickedUp, courierId);

        var success = Assert.IsType<OrderTransitionResult.Success>(result);
        Assert.Equal(DeliveryOrderStatus.PickedUp, success.Order.Status);
        Assert.NotNull(success.Order.PickedUpAt);
    }

    [Fact]
    public async Task TransitionAsync_WithAnIllegalStep_ReturnsInvalidTransitionWithAllowedStates()
    {
        using var testDb = new SqliteTestDatabase();
        var (order, courierId) = await SeedAssignedOrderAsync(testDb);

        var sut = new OrderTransitionService(testDb.Context, TimeProvider.System);
        var result = await sut.TransitionAsync(order.Id, DeliveryOrderStatus.InTransit, courierId);

        var invalid = Assert.IsType<OrderTransitionResult.InvalidTransition>(result);
        Assert.Equal(DeliveryOrderStatus.Assigned, invalid.CurrentStatus);
        Assert.Contains(DeliveryOrderStatus.PickedUp, invalid.AllowedNextStates);
    }

    [Fact]
    public async Task TransitionAsync_ByACourierWhoDoesNotOwnTheOrder_ReturnsForbidden()
    {
        using var testDb = new SqliteTestDatabase();
        var (order, _) = await SeedAssignedOrderAsync(testDb);
        var otherCourier = TestEntities.NewCourier("Downtown");
        testDb.Context.Couriers.Add(otherCourier);
        await testDb.Context.SaveChangesAsync();

        var sut = new OrderTransitionService(testDb.Context, TimeProvider.System);
        var result = await sut.TransitionAsync(order.Id, DeliveryOrderStatus.PickedUp, otherCourier.Id);

        Assert.IsType<OrderTransitionResult.Forbidden>(result);
    }

    [Fact]
    public async Task TransitionAsync_ByADispatcher_BypassesOwnershipCheck()
    {
        using var testDb = new SqliteTestDatabase();
        var (order, _) = await SeedAssignedOrderAsync(testDb);

        var sut = new OrderTransitionService(testDb.Context, TimeProvider.System);
        var result = await sut.TransitionAsync(order.Id, DeliveryOrderStatus.Cancelled, actingCourierId: null);

        var success = Assert.IsType<OrderTransitionResult.Success>(result);
        Assert.Equal(DeliveryOrderStatus.Cancelled, success.Order.Status);
    }

    [Fact]
    public async Task TransitionAsync_WhenOrderDoesNotExist_ReturnsOrderNotFound()
    {
        using var testDb = new SqliteTestDatabase();
        var sut = new OrderTransitionService(testDb.Context, TimeProvider.System);

        var result = await sut.TransitionAsync(999, DeliveryOrderStatus.PickedUp, actingCourierId: 1);

        Assert.IsType<OrderTransitionResult.OrderNotFound>(result);
    }

    [Fact]
    public async Task DeliverAsync_FromInTransit_MarksDeliveredAndStampsIdempotencyKey()
    {
        using var testDb = new SqliteTestDatabase();
        var (order, courierId) = await SeedInTransitOrderAsync(testDb);

        var sut = new OrderTransitionService(testDb.Context, TimeProvider.System);
        var result = await sut.DeliverAsync(order.Id, courierId, "key-1");

        var success = Assert.IsType<OrderTransitionResult.Success>(result);
        Assert.Equal(DeliveryOrderStatus.Delivered, success.Order.Status);
        Assert.NotNull(success.Order.DeliveredAt);
        Assert.Equal("key-1", success.Order.LastDeliveryIdempotencyKey);
    }

    [Fact]
    public async Task DeliverAsync_CalledTwiceWithAnyKey_IsIdempotentAndDoesNotDuplicateStatusEvents()
    {
        using var testDb = new SqliteTestDatabase();
        var (order, courierId) = await SeedInTransitOrderAsync(testDb);
        var sut = new OrderTransitionService(testDb.Context, TimeProvider.System);

        var first = await sut.DeliverAsync(order.Id, courierId, "key-1");
        var second = await sut.DeliverAsync(order.Id, courierId, "key-2");

        Assert.IsType<OrderTransitionResult.Success>(first);
        var secondSuccess = Assert.IsType<OrderTransitionResult.Success>(second);
        Assert.Equal(DeliveryOrderStatus.Delivered, secondSuccess.Order.Status);

        var deliveredEvents = testDb.Context.OrderStatusEvents
            .Where(e => e.DeliveryOrderId == order.Id && e.ToStatus == DeliveryOrderStatus.Delivered)
            .ToList();
        Assert.Single(deliveredEvents);
    }

    [Fact]
    public async Task DeliverAsync_ByACourierWhoDoesNotOwnTheOrder_ReturnsForbidden()
    {
        using var testDb = new SqliteTestDatabase();
        var (order, _) = await SeedInTransitOrderAsync(testDb);
        var otherCourier = TestEntities.NewCourier("Downtown");
        testDb.Context.Couriers.Add(otherCourier);
        await testDb.Context.SaveChangesAsync();

        var sut = new OrderTransitionService(testDb.Context, TimeProvider.System);
        var result = await sut.DeliverAsync(order.Id, otherCourier.Id, "key-1");

        Assert.IsType<OrderTransitionResult.Forbidden>(result);
    }

    [Fact]
    public async Task DeliverAsync_WhenOrderIsNotYetInTransit_ReturnsInvalidTransition()
    {
        using var testDb = new SqliteTestDatabase();
        var (order, courierId) = await SeedAssignedOrderAsync(testDb);

        var sut = new OrderTransitionService(testDb.Context, TimeProvider.System);
        var result = await sut.DeliverAsync(order.Id, courierId, "key-1");

        Assert.IsType<OrderTransitionResult.InvalidTransition>(result);
    }

    private static async Task<(DeliveryOrder Order, int CourierId)> SeedAssignedOrderAsync(SqliteTestDatabase testDb)
    {
        var courier = TestEntities.NewCourier("Downtown");
        testDb.Context.Couriers.Add(courier);
        await testDb.Context.SaveChangesAsync();

        var order = TestEntities.NewPendingOrder("Downtown");
        order.Status = DeliveryOrderStatus.Assigned;
        order.CourierId = courier.Id;
        testDb.Context.DeliveryOrders.Add(order);
        await testDb.Context.SaveChangesAsync();
        return (order, courier.Id);
    }

    private static async Task<(DeliveryOrder Order, int CourierId)> SeedInTransitOrderAsync(SqliteTestDatabase testDb)
    {
        var courier = TestEntities.NewCourier("Downtown");
        testDb.Context.Couriers.Add(courier);
        await testDb.Context.SaveChangesAsync();

        var order = TestEntities.NewPendingOrder("Downtown");
        order.Status = DeliveryOrderStatus.InTransit;
        order.CourierId = courier.Id;
        order.PickedUpAt = DateTime.UtcNow;
        testDb.Context.DeliveryOrders.Add(order);
        await testDb.Context.SaveChangesAsync();
        return (order, courier.Id);
    }
}
