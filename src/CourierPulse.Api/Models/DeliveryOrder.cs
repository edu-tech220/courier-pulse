namespace CourierPulse.Api.Models;

public class DeliveryOrder : IConcurrencyAware
{
    public int Id { get; set; }

    /// <summary>Public-facing code used on the anonymous tracking endpoint, e.g. "CP-8F3K2Q".</summary>
    public string TrackingCode { get; set; } = string.Empty;

    public string PickupAddress { get; set; } = string.Empty;

    public string DropoffAddress { get; set; } = string.Empty;

    /// <summary>Service zone the order falls in; couriers are only auto-assigned orders in their own zone.</summary>
    public string Zone { get; set; } = string.Empty;

    public decimal PackageWeightKg { get; set; }

    public DateTime PromisedWindowStart { get; set; }

    public DateTime PromisedWindowEnd { get; set; }

    public DeliveryOrderStatus Status { get; set; } = DeliveryOrderStatus.Pending;

    public int? CourierId { get; set; }

    public Courier? Courier { get; set; }

    public string? Notes { get; set; }

    /// <summary>Idempotency key used by the last successful call to the deliver endpoint.</summary>
    public string? LastDeliveryIdempotencyKey { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? AssignedAt { get; set; }

    public DateTime? PickedUpAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<OrderStatusEvent> StatusEvents { get; set; } = new List<OrderStatusEvent>();
}
