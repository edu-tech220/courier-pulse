using CourierPulse.Api.Models;

namespace CourierPulse.Api.Services;

/// <summary>
/// Encodes the valid lifecycle of a <see cref="DeliveryOrder"/>:
///
///   Pending -> Assigned -> PickedUp -> InTransit -> Delivered
///      |          |                        |
///      v          v                        v
///  Cancelled  Cancelled                  Failed
///
/// Delivered, Failed and Cancelled are terminal: no further transitions are allowed from them.
/// </summary>
public static class OrderStateMachine
{
    private static readonly Dictionary<DeliveryOrderStatus, DeliveryOrderStatus[]> Transitions = new()
    {
        [DeliveryOrderStatus.Pending] = [DeliveryOrderStatus.Assigned, DeliveryOrderStatus.Cancelled],
        [DeliveryOrderStatus.Assigned] = [DeliveryOrderStatus.PickedUp, DeliveryOrderStatus.Cancelled],
        [DeliveryOrderStatus.PickedUp] = [DeliveryOrderStatus.InTransit],
        [DeliveryOrderStatus.InTransit] = [DeliveryOrderStatus.Delivered, DeliveryOrderStatus.Failed],
        [DeliveryOrderStatus.Delivered] = [],
        [DeliveryOrderStatus.Failed] = [],
        [DeliveryOrderStatus.Cancelled] = [],
    };

    public static IReadOnlyCollection<DeliveryOrderStatus> AllowedNextStates(DeliveryOrderStatus from) =>
        Transitions[from];

    public static bool CanTransition(DeliveryOrderStatus from, DeliveryOrderStatus to) =>
        Transitions[from].Contains(to);

    public static bool IsTerminal(DeliveryOrderStatus status) =>
        Transitions[status].Length == 0;
}
