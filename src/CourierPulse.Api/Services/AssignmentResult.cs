using CourierPulse.Api.Models;

namespace CourierPulse.Api.Services;

public abstract record AssignmentResult
{
    private AssignmentResult()
    {
    }

    public sealed record Success(DeliveryOrder Order) : AssignmentResult;

    public sealed record OrderNotFound : AssignmentResult;

    public sealed record OrderNotPending(DeliveryOrderStatus CurrentStatus) : AssignmentResult;

    public sealed record NoCourierAvailable : AssignmentResult;

    public sealed record ConcurrencyConflict : AssignmentResult;
}
