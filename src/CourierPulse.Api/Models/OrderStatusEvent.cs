namespace CourierPulse.Api.Models;

/// <summary>Append-only audit trail entry recording one status transition of a delivery order.</summary>
public class OrderStatusEvent
{
    public int Id { get; set; }

    public int DeliveryOrderId { get; set; }

    public DeliveryOrder? DeliveryOrder { get; set; }

    public DeliveryOrderStatus FromStatus { get; set; }

    public DeliveryOrderStatus ToStatus { get; set; }

    public DateTime ChangedAt { get; set; }

    public int? ChangedByCourierId { get; set; }
}
