using CourierPulse.Api.Models;

namespace CourierPulse.Api.Services;

public interface IOrderTransitionService
{
    /// <summary>
    /// Moves an order to <paramref name="toStatus"/> if the transition is legal for its
    /// current status. When <paramref name="actingCourierId"/> is set, the order must
    /// currently be assigned to that courier (dispatchers/admins pass null to bypass the
    /// ownership check).
    /// </summary>
    Task<OrderTransitionResult> TransitionAsync(int orderId, DeliveryOrderStatus toStatus, int? actingCourierId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an order Delivered. Idempotent: if the order is already Delivered this returns
    /// the existing order instead of failing, so a retried request from a flaky courier app
    /// never double-processes the delivery.
    /// </summary>
    Task<OrderTransitionResult> DeliverAsync(int orderId, int actingCourierId, string idempotencyKey, CancellationToken cancellationToken = default);
}
