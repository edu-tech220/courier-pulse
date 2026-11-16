using CourierPulse.Api.Models;
using CourierPulse.Api.Services;
using CourierPulse.Api.Tests.Infrastructure;

namespace CourierPulse.Api.Tests.Services;

public class AssignmentServiceTests
{
    [Fact]
    public async Task AssignNextAvailableCourierAsync_WithAvailableCourierInZone_AssignsOrder()
    {
        using var testDb = new SqliteTestDatabase();
        var courier = TestEntities.NewCourier("Downtown");
        var order = TestEntities.NewPendingOrder("Downtown");
        testDb.Context.Couriers.Add(courier);
        testDb.Context.DeliveryOrders.Add(order);
        await testDb.Context.SaveChangesAsync();

        var sut = new AssignmentService(testDb.Context, TimeProvider.System);
        var result = await sut.AssignNextAvailableCourierAsync(order.Id);

        var success = Assert.IsType<AssignmentResult.Success>(result);
        Assert.Equal(courier.Id, success.Order.CourierId);
        Assert.Equal(DeliveryOrderStatus.Assigned, success.Order.Status);
        Assert.NotNull(success.Order.AssignedAt);
    }

    [Fact]
    public async Task AssignNextAvailableCourierAsync_RecordsAStatusEvent()
    {
        using var testDb = new SqliteTestDatabase();
        var courier = TestEntities.NewCourier("Downtown");
        var order = TestEntities.NewPendingOrder("Downtown");
        testDb.Context.Couriers.Add(courier);
        testDb.Context.DeliveryOrders.Add(order);
        await testDb.Context.SaveChangesAsync();

        var sut = new AssignmentService(testDb.Context, TimeProvider.System);
        await sut.AssignNextAvailableCourierAsync(order.Id);

        var events = testDb.Context.OrderStatusEvents.Where(e => e.DeliveryOrderId == order.Id).ToList();
        var @event = Assert.Single(events);
        Assert.Equal(DeliveryOrderStatus.Pending, @event.FromStatus);
        Assert.Equal(DeliveryOrderStatus.Assigned, @event.ToStatus);
    }

    [Fact]
    public async Task AssignNextAvailableCourierAsync_PicksTheLeastLoadedCourier()
    {
        using var testDb = new SqliteTestDatabase();
        var busyCourier = TestEntities.NewCourier("Downtown");
        var freeCourier = TestEntities.NewCourier("Downtown");
        testDb.Context.Couriers.AddRange(busyCourier, freeCourier);
        await testDb.Context.SaveChangesAsync();

        var existingActiveOrder = TestEntities.NewPendingOrder("Downtown");
        existingActiveOrder.Status = DeliveryOrderStatus.Assigned;
        existingActiveOrder.CourierId = busyCourier.Id;
        testDb.Context.DeliveryOrders.Add(existingActiveOrder);

        var newOrder = TestEntities.NewPendingOrder("Downtown");
        testDb.Context.DeliveryOrders.Add(newOrder);
        await testDb.Context.SaveChangesAsync();

        var sut = new AssignmentService(testDb.Context, TimeProvider.System);
        var result = await sut.AssignNextAvailableCourierAsync(newOrder.Id);

        var success = Assert.IsType<AssignmentResult.Success>(result);
        Assert.Equal(freeCourier.Id, success.Order.CourierId);
    }

    [Fact]
    public async Task AssignNextAvailableCourierAsync_IgnoresCouriersInADifferentZone()
    {
        using var testDb = new SqliteTestDatabase();
        testDb.Context.Couriers.Add(TestEntities.NewCourier("North"));
        var order = TestEntities.NewPendingOrder("Downtown");
        testDb.Context.DeliveryOrders.Add(order);
        await testDb.Context.SaveChangesAsync();

        var sut = new AssignmentService(testDb.Context, TimeProvider.System);
        var result = await sut.AssignNextAvailableCourierAsync(order.Id);

        Assert.IsType<AssignmentResult.NoCourierAvailable>(result);
    }

    [Theory]
    [InlineData(CourierAvailability.Busy)]
    [InlineData(CourierAvailability.Offline)]
    public async Task AssignNextAvailableCourierAsync_IgnoresCouriersThatAreNotAvailable(CourierAvailability availability)
    {
        using var testDb = new SqliteTestDatabase();
        testDb.Context.Couriers.Add(TestEntities.NewCourier("Downtown", availability));
        var order = TestEntities.NewPendingOrder("Downtown");
        testDb.Context.DeliveryOrders.Add(order);
        await testDb.Context.SaveChangesAsync();

        var sut = new AssignmentService(testDb.Context, TimeProvider.System);
        var result = await sut.AssignNextAvailableCourierAsync(order.Id);

        Assert.IsType<AssignmentResult.NoCourierAvailable>(result);
    }

    [Fact]
    public async Task AssignNextAvailableCourierAsync_WhenOrderDoesNotExist_ReturnsOrderNotFound()
    {
        using var testDb = new SqliteTestDatabase();

        var sut = new AssignmentService(testDb.Context, TimeProvider.System);
        var result = await sut.AssignNextAvailableCourierAsync(999);

        Assert.IsType<AssignmentResult.OrderNotFound>(result);
    }

    [Fact]
    public async Task AssignNextAvailableCourierAsync_WhenOrderIsNotPending_ReturnsOrderNotPending()
    {
        using var testDb = new SqliteTestDatabase();
        var order = TestEntities.NewPendingOrder("Downtown");
        order.Status = DeliveryOrderStatus.Cancelled;
        testDb.Context.DeliveryOrders.Add(order);
        await testDb.Context.SaveChangesAsync();

        var sut = new AssignmentService(testDb.Context, TimeProvider.System);
        var result = await sut.AssignNextAvailableCourierAsync(order.Id);

        var notPending = Assert.IsType<AssignmentResult.OrderNotPending>(result);
        Assert.Equal(DeliveryOrderStatus.Cancelled, notPending.CurrentStatus);
    }

    [Fact]
    public async Task AssignNextAvailableCourierAsync_WhenAnotherRequestAssignsFirst_RetriesAndDetectsIt()
    {
        using var testDb = new SqliteTestDatabase();
        var courier = TestEntities.NewCourier("Downtown");
        var order = TestEntities.NewPendingOrder("Downtown");
        testDb.Context.Couriers.Add(courier);
        testDb.Context.DeliveryOrders.Add(order);
        await testDb.Context.SaveChangesAsync();

        // A second dispatcher's request assigns the order first, through a separate DbContext
        // sharing the same underlying database - testDb.Context still has the pre-race state
        // tracked locally, so the service only learns about it via the concurrency exception.
        await using var racingContext = testDb.CreateAdditionalContext();
        var racingOrder = await racingContext.DeliveryOrders.FindAsync(order.Id);
        racingOrder!.Status = DeliveryOrderStatus.Assigned;
        racingOrder.CourierId = courier.Id;
        await racingContext.SaveChangesAsync();

        var sut = new AssignmentService(testDb.Context, TimeProvider.System);
        var result = await sut.AssignNextAvailableCourierAsync(order.Id);

        var notPending = Assert.IsType<AssignmentResult.OrderNotPending>(result);
        Assert.Equal(DeliveryOrderStatus.Assigned, notPending.CurrentStatus);
    }
}
