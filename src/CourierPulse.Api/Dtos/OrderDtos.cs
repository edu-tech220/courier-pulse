using System.ComponentModel.DataAnnotations;
using CourierPulse.Api.Models;

namespace CourierPulse.Api.Dtos;

public record CreateOrderRequest(
    [property: Required, StringLength(300, MinimumLength = 3)] string PickupAddress,
    [property: Required, StringLength(300, MinimumLength = 3)] string DropoffAddress,
    [property: Required, StringLength(100, MinimumLength = 1)] string Zone,
    [property: Range(0.01, 1000)] decimal PackageWeightKg,
    [property: Required] DateTime PromisedWindowStart,
    [property: Required] DateTime PromisedWindowEnd,
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

public record TransitionRequest([property: Required] DeliveryOrderStatus ToStatus);

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
