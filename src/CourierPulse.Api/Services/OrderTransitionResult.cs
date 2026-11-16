using CourierPulse.Api.Models;

namespace CourierPulse.Api.Services;

public abstract record OrderTransitionResult
{
    private OrderTransitionResult()
    {
    }

    public sealed record Success(DeliveryOrder Order) : OrderTransitionResult;

    public sealed record OrderNotFound : OrderTransitionResult;

    public sealed record InvalidTransition(DeliveryOrderStatus CurrentStatus, IReadOnlyCollection<DeliveryOrderStatus> AllowedNextStates) : OrderTransitionResult;

    public sealed record Forbidden : OrderTransitionResult;

    public sealed record ConcurrencyConflict : OrderTransitionResult;
}
