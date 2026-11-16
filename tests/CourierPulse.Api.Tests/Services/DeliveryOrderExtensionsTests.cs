using CourierPulse.Api.Models;
using CourierPulse.Api.Services;
using CourierPulse.Api.Tests.Infrastructure;

namespace CourierPulse.Api.Tests.Services;

public class DeliveryOrderExtensionsTests
{
    private static readonly DateTime WindowEnd = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void IsOverdue_CancelledOrder_IsNeverOverdueEvenLongAfterWindow()
    {
        var order = TestEntities.NewPendingOrder(promisedWindowEnd: WindowEnd);
        order.Status = DeliveryOrderStatus.Cancelled;

        Assert.False(order.IsOverdue(WindowEnd.AddDays(5)));
    }

    [Theory]
    [InlineData(DeliveryOrderStatus.Pending)]
    [InlineData(DeliveryOrderStatus.Assigned)]
    [InlineData(DeliveryOrderStatus.PickedUp)]
    [InlineData(DeliveryOrderStatus.InTransit)]
    [InlineData(DeliveryOrderStatus.Failed)]
    public void IsOverdue_ActiveOrFailedOrder_IsOverdueOnceWindowHasPassed(DeliveryOrderStatus status)
    {
        var order = TestEntities.NewPendingOrder(promisedWindowEnd: WindowEnd);
        order.Status = status;

        Assert.True(order.IsOverdue(WindowEnd.AddMinutes(1)));
        Assert.False(order.IsOverdue(WindowEnd.AddMinutes(-1)));
    }

    [Fact]
    public void IsOverdue_DeliveredBeforeWindowEnd_IsNotOverdue()
    {
        var order = TestEntities.NewPendingOrder(promisedWindowEnd: WindowEnd);
        order.Status = DeliveryOrderStatus.Delivered;
        order.DeliveredAt = WindowEnd.AddMinutes(-5);

        Assert.False(order.IsOverdue(WindowEnd.AddDays(1)));
    }

    [Fact]
    public void IsOverdue_DeliveredAfterWindowEnd_IsOverdue()
    {
        var order = TestEntities.NewPendingOrder(promisedWindowEnd: WindowEnd);
        order.Status = DeliveryOrderStatus.Delivered;
        order.DeliveredAt = WindowEnd.AddMinutes(5);

        Assert.True(order.IsOverdue(WindowEnd.AddDays(1)));
    }
}
