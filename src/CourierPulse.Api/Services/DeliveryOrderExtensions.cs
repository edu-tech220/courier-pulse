using CourierPulse.Api.Models;

namespace CourierPulse.Api.Services;

public static class DeliveryOrderExtensions
{
    /// <summary>
    /// An order is overdue once the promised window has elapsed without a (timely) delivery.
    /// Cancelled orders are never overdue; a Delivered order is overdue only if it was
    /// delivered after its promised window closed.
    /// </summary>
    public static bool IsOverdue(this DeliveryOrder order, DateTime utcNow) => order.Status switch
    {
        DeliveryOrderStatus.Cancelled => false,
        DeliveryOrderStatus.Delivered => order.DeliveredAt is { } deliveredAt && deliveredAt > order.PromisedWindowEnd,
        _ => utcNow > order.PromisedWindowEnd,
    };
}
