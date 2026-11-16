namespace CourierPulse.Api.Models;

public class Courier : IConcurrencyAware
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public CourierRole Role { get; set; }

    /// <summary>
    /// Service zone the courier operates in. Only meaningful for the Courier role;
    /// used to pick auto-assignment candidates for orders in the same zone.
    /// </summary>
    public string Zone { get; set; } = string.Empty;

    public CourierAvailability Availability { get; set; } = CourierAvailability.Offline;

    public DateTime CreatedAt { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<DeliveryOrder> AssignedOrders { get; set; } = new List<DeliveryOrder>();
}
