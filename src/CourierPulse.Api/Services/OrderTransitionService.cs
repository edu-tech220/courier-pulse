using CourierPulse.Api.Data;
using CourierPulse.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CourierPulse.Api.Services;

public class OrderTransitionService : IOrderTransitionService
{
    private const int MaxConcurrencyRetries = 3;

    private readonly CourierPulseDbContext _db;
    private readonly TimeProvider _timeProvider;

    public OrderTransitionService(CourierPulseDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public Task<OrderTransitionResult> TransitionAsync(int orderId, DeliveryOrderStatus toStatus, int? actingCourierId, CancellationToken cancellationToken = default) =>
        ExecuteWithConcurrencyRetryAsync(orderId, order =>
        {
            if (actingCourierId is not null && order.CourierId != actingCourierId)
            {
                return new OrderTransitionResult.Forbidden();
            }

            if (!OrderStateMachine.CanTransition(order.Status, toStatus))
            {
                return new OrderTransitionResult.InvalidTransition(order.Status, OrderStateMachine.AllowedNextStates(order.Status));
            }

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var from = order.Status;
            order.Status = toStatus;
            if (toStatus == DeliveryOrderStatus.PickedUp)
            {
                order.PickedUpAt = now;
            }

            _db.OrderStatusEvents.Add(new OrderStatusEvent
            {
                DeliveryOrderId = order.Id,
                FromStatus = from,
                ToStatus = toStatus,
                ChangedAt = now,
                ChangedByCourierId = actingCourierId,
            });

            return null;
        }, cancellationToken);

    public Task<OrderTransitionResult> DeliverAsync(int orderId, int actingCourierId, string idempotencyKey, CancellationToken cancellationToken = default) =>
        ExecuteWithConcurrencyRetryAsync(orderId, order =>
        {
            if (order.CourierId != actingCourierId)
            {
                return new OrderTransitionResult.Forbidden();
            }

            // Already delivered: treat a retried request (same or lost idempotency key) as a
            // no-op success instead of re-running the delivery side effects.
            if (order.Status == DeliveryOrderStatus.Delivered)
            {
                return new OrderTransitionResult.Success(order);
            }

            if (!OrderStateMachine.CanTransition(order.Status, DeliveryOrderStatus.Delivered))
            {
                return new OrderTransitionResult.InvalidTransition(order.Status, OrderStateMachine.AllowedNextStates(order.Status));
            }

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var from = order.Status;
            order.Status = DeliveryOrderStatus.Delivered;
            order.DeliveredAt = now;
            order.LastDeliveryIdempotencyKey = idempotencyKey;

            _db.OrderStatusEvents.Add(new OrderStatusEvent
            {
                DeliveryOrderId = order.Id,
                FromStatus = from,
                ToStatus = DeliveryOrderStatus.Delivered,
                ChangedAt = now,
                ChangedByCourierId = actingCourierId,
            });

            return null;
        }, cancellationToken);

    /// <summary>
    /// Loads the order, lets <paramref name="apply"/> validate and mutate it, then saves.
    /// <paramref name="apply"/> returns a result to short-circuit without saving (e.g. a
    /// validation failure) or null to proceed with SaveChanges. On a concurrency conflict the
    /// change tracker is cleared and the whole thing retries against fresh data.
    /// </summary>
    private async Task<OrderTransitionResult> ExecuteWithConcurrencyRetryAsync(
        int orderId,
        Func<DeliveryOrder, OrderTransitionResult?> apply,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxConcurrencyRetries; attempt++)
        {
            var order = await _db.DeliveryOrders.FindAsync([orderId], cancellationToken);
            if (order is null)
            {
                return new OrderTransitionResult.OrderNotFound();
            }

            var shortCircuit = apply(order);
            if (shortCircuit is not null)
            {
                return shortCircuit;
            }

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
                return new OrderTransitionResult.Success(order);
            }
            catch (DbUpdateConcurrencyException)
            {
                _db.ChangeTracker.Clear();
            }
        }

        return new OrderTransitionResult.ConcurrencyConflict();
    }
}
