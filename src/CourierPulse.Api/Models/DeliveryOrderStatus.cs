namespace CourierPulse.Api.Models;

public enum DeliveryOrderStatus
{
    Pending,
    Assigned,
    PickedUp,
    InTransit,
    Delivered,
    Failed,
    Cancelled
}
