using CourierPulse.Api.Data;
using CourierPulse.Api.Dtos;
using CourierPulse.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace CourierPulse.Api.Controllers;

/// <summary>Anonymous, rate-limited endpoint so a recipient can track a parcel by its code.</summary>
[ApiController]
[Route("api/public/tracking")]
[AllowAnonymous]
[EnableRateLimiting("public-tracking")]
public class PublicTrackingController : ControllerBase
{
    private readonly CourierPulseDbContext _db;
    private readonly TimeProvider _timeProvider;

    public PublicTrackingController(CourierPulseDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    [HttpGet("{trackingCode}")]
    [ProducesResponseType(typeof(PublicTrackingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PublicTrackingResponse>> GetByTrackingCode(string trackingCode)
    {
        var order = await _db.DeliveryOrders.AsNoTracking().FirstOrDefaultAsync(o => o.TrackingCode == trackingCode);
        if (order is null)
        {
            return NotFound();
        }

        var timeline = await _db.OrderStatusEvents
            .AsNoTracking()
            .Where(e => e.DeliveryOrderId == order.Id)
            .OrderBy(e => e.ChangedAt)
            .Select(e => new OrderStatusEventResponse(e.FromStatus, e.ToStatus, e.ChangedAt, e.ChangedByCourierId))
            .ToListAsync();

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        return Ok(new PublicTrackingResponse(
            order.TrackingCode,
            order.Status,
            order.Zone,
            order.PromisedWindowStart,
            order.PromisedWindowEnd,
            order.IsOverdue(now),
            timeline));
    }
}
