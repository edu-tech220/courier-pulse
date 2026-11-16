namespace CourierPulse.Web.ApiClient;

// Mirrors the wire contract of CourierPulse.Api (a separate microservice reached only over
// HTTP) - duplicated on purpose rather than referencing the Api project, so the two services
// stay independently deployable.

public enum DeliveryOrderStatus
{
    Pending,
    Assigned,
    PickedUp,
    InTransit,
    Delivered,
    Failed,
    Cancelled,
}

public enum CourierRole
{
    Admin,
    Dispatcher,
    Courier,
}

public enum CourierAvailability
{
    Offline,
    Available,
    Busy,
}

public record LoginRequest(string Email, string Password);

public record LoginResponse(string Token, DateTime ExpiresAtUtc, string FullName, string Role);

public record CreateOrderRequest(
    string PickupAddress,
    string DropoffAddress,
    string Zone,
    decimal PackageWeightKg,
    DateTime PromisedWindowStart,
    DateTime PromisedWindowEnd,
    string? Notes);

public record OrderResponse(
    int Id,
    string TrackingCode,
    string PickupAddress,
    string DropoffAddress,
    string Zone,
    decimal PackageWeightKg,
    DateTime PromisedWindowStart,
    DateTime PromisedWindowEnd,
    DeliveryOrderStatus Status,
    int? CourierId,
    string? CourierName,
    bool IsOverdue,
    string? Notes,
    DateTime CreatedAt,
    DateTime? AssignedAt,
    DateTime? PickedUpAt,
    DateTime? DeliveredAt);

public record TransitionRequest(DeliveryOrderStatus ToStatus);

public record OrderStatusEventResponse(
    DeliveryOrderStatus FromStatus,
    DeliveryOrderStatus ToStatus,
    DateTime ChangedAt,
    int? ChangedByCourierId);

public record PublicTrackingResponse(
    string TrackingCode,
    DeliveryOrderStatus Status,
    string Zone,
    DateTime PromisedWindowStart,
    DateTime PromisedWindowEnd,
    bool IsOverdue,
    IReadOnlyCollection<OrderStatusEventResponse> Timeline);

public record CourierResponse(
    int Id,
    string FullName,
    string Email,
    CourierRole Role,
    string Zone,
    CourierAvailability Availability);

public record CreateCourierRequest(string FullName, string Email, string Password, CourierRole Role, string? Zone);

public record UpdateAvailabilityRequest(CourierAvailability Availability);

public record PagedResult<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, int TotalCount);
