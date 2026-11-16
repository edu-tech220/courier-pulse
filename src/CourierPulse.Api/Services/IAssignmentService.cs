namespace CourierPulse.Api.Services;

public interface IAssignmentService
{
    /// <summary>
    /// Assigns a pending order to the least-loaded available courier in the order's zone.
    /// Safe under concurrent calls: if another request assigns or modifies the same order
    /// first, this retries the candidate search instead of corrupting state.
    /// </summary>
    Task<AssignmentResult> AssignNextAvailableCourierAsync(int orderId, CancellationToken cancellationToken = default);
}
