using CourierPulse.Api.Data;
using CourierPulse.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CourierPulse.Api.Services;

public class AssignmentService : IAssignmentService
{
    private const int MaxConcurrencyRetries = 3;

    private readonly CourierPulseDbContext _db;
    private readonly TimeProvider _timeProvider;

    public AssignmentService(CourierPulseDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<AssignmentResult> AssignNextAvailableCourierAsync(int orderId, CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < MaxConcurrencyRetries; attempt++)
        {
            var order = await _db.DeliveryOrders.FindAsync([orderId], cancellationToken);
            if (order is null)
            {
                return new AssignmentResult.OrderNotFound();
            }

            if (order.Status != DeliveryOrderStatus.Pending)
            {
                return new AssignmentResult.OrderNotPending(order.Status);
            }

            var candidateId = await FindLeastLoadedCourierIdAsync(order.Zone, cancellationToken);
            if (candidateId is null)
            {
                return new AssignmentResult.NoCourierAvailable();
            }

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            order.CourierId = candidateId;
            order.Status = DeliveryOrderStatus.Assigned;
            order.AssignedAt = now;

            _db.OrderStatusEvents.Add(new OrderStatusEvent
            {
                DeliveryOrderId = order.Id,
                FromStatus = DeliveryOrderStatus.Pending,
                ToStatus = DeliveryOrderStatus.Assigned,
                ChangedAt = now,
                ChangedByCourierId = candidateId,
            });

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
                return new AssignmentResult.Success(order);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Another dispatcher raced us for the same order; drop the stale tracked
                // state and retry the whole candidate search against fresh data.
                _db.ChangeTracker.Clear();
            }
        }

        return new AssignmentResult.ConcurrencyConflict();
    }

    private async Task<int?> FindLeastLoadedCourierIdAsync(string zone, CancellationToken cancellationToken)
    {
        var activeStatuses = new[] { DeliveryOrderStatus.Assigned, DeliveryOrderStatus.PickedUp, DeliveryOrderStatus.InTransit };

        var candidate = await _db.Couriers
            .Where(c => c.Role == CourierRole.Courier
                && c.Availability == CourierAvailability.Available
                && c.Zone == zone)
            .Select(c => new
            {
                c.Id,
                ActiveOrderCount = _db.DeliveryOrders.Count(o => o.CourierId == c.Id && activeStatuses.Contains(o.Status)),
            })
            .OrderBy(c => c.ActiveOrderCount)
            .ThenBy(c => c.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return candidate?.Id;
    }
}
