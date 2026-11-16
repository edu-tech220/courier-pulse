using System.Security.Claims;
using CourierPulse.Api.Data;
using CourierPulse.Api.Dtos;
using CourierPulse.Api.Models;
using CourierPulse.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CourierPulse.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly CourierPulseDbContext _db;
    private readonly IAssignmentService _assignmentService;
    private readonly IOrderTransitionService _transitionService;
    private readonly TimeProvider _timeProvider;

    public OrdersController(
        CourierPulseDbContext db,
        IAssignmentService assignmentService,
        IOrderTransitionService transitionService,
        TimeProvider timeProvider)
    {
        _db = db;
        _assignmentService = assignmentService;
        _transitionService = transitionService;
        _timeProvider = timeProvider;
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Dispatcher")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<OrderResponse>> Create(CreateOrderRequest request)
    {
        if (request.PromisedWindowEnd <= request.PromisedWindowStart)
        {
            ModelState.AddModelError(nameof(request.PromisedWindowEnd), "The promised window end must be after its start.");
            return ValidationProblem(ModelState);
        }

        var order = new DeliveryOrder
        {
            TrackingCode = TrackingCodeGenerator.Generate(),
            PickupAddress = request.PickupAddress,
            DropoffAddress = request.DropoffAddress,
            Zone = request.Zone,
            PackageWeightKg = request.PackageWeightKg,
            PromisedWindowStart = request.PromisedWindowStart,
            PromisedWindowEnd = request.PromisedWindowEnd,
            Notes = request.Notes,
            Status = DeliveryOrderStatus.Pending,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
        };

        _db.DeliveryOrders.Add(order);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, await MapToResponseAsync(order));
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Dispatcher")]
    [ProducesResponseType(typeof(PagedResult<OrderResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<OrderResponse>>> GetAll(
        [FromQuery] DeliveryOrderStatus? status,
        [FromQuery] string? zone,
        [FromQuery] bool overdueOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var query = _db.DeliveryOrders.AsNoTracking().AsQueryable();

        if (status is not null)
        {
            query = query.Where(o => o.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(zone))
        {
            query = query.Where(o => o.Zone == zone);
        }

        // Mirrors DeliveryOrderExtensions.IsOverdue, inlined (rather than calling a shared
        // method) so EF Core can translate it into SQL instead of throwing at query time.
        if (overdueOnly)
        {
            query = query.Where(o => o.Status != DeliveryOrderStatus.Cancelled &&
                ((o.Status == DeliveryOrderStatus.Delivered && o.DeliveredAt != null && o.DeliveredAt > o.PromisedWindowEnd) ||
                 (o.Status != DeliveryOrderStatus.Delivered && now > o.PromisedWindowEnd)));
        }

        var totalCount = await query.CountAsync();

        // Single projected query (LEFT JOIN to Couriers for the name) - no per-row round trips.
        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderResponse(
                o.Id, o.TrackingCode, o.PickupAddress, o.DropoffAddress, o.Zone, o.PackageWeightKg,
                o.PromisedWindowStart, o.PromisedWindowEnd, o.Status, o.CourierId,
                o.Courier == null ? null : o.Courier.FullName,
                o.Status != DeliveryOrderStatus.Cancelled &&
                    ((o.Status == DeliveryOrderStatus.Delivered && o.DeliveredAt != null && o.DeliveredAt > o.PromisedWindowEnd) ||
                     (o.Status != DeliveryOrderStatus.Delivered && now > o.PromisedWindowEnd)),
                o.Notes, o.CreatedAt, o.AssignedAt, o.PickedUpAt, o.DeliveredAt))
            .ToListAsync();

        return Ok(new PagedResult<OrderResponse>(items, page, pageSize, totalCount));
    }

    [HttpGet("mine")]
    [Authorize(Roles = "Courier")]
    [ProducesResponseType(typeof(PagedResult<OrderResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<OrderResponse>>> GetMine([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var courierId = GetCourierId();

        var query = _db.DeliveryOrders.AsNoTracking().Where(o => o.CourierId == courierId);
        var totalCount = await query.CountAsync();
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderResponse(
                o.Id, o.TrackingCode, o.PickupAddress, o.DropoffAddress, o.Zone, o.PackageWeightKg,
                o.PromisedWindowStart, o.PromisedWindowEnd, o.Status, o.CourierId,
                o.Courier == null ? null : o.Courier.FullName,
                o.Status != DeliveryOrderStatus.Cancelled &&
                    ((o.Status == DeliveryOrderStatus.Delivered && o.DeliveredAt != null && o.DeliveredAt > o.PromisedWindowEnd) ||
                     (o.Status != DeliveryOrderStatus.Delivered && now > o.PromisedWindowEnd)),
                o.Notes, o.CreatedAt, o.AssignedAt, o.PickedUpAt, o.DeliveredAt))
            .ToListAsync();

        return Ok(new PagedResult<OrderResponse>(items, page, pageSize, totalCount));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<OrderResponse>> GetById(int id)
    {
        var order = await _db.DeliveryOrders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
        if (order is null)
        {
            return NotFound();
        }

        if (User.IsInRole("Courier") && order.CourierId != GetCourierId())
        {
            return Forbid();
        }

        return Ok(await MapToResponseAsync(order));
    }

    [HttpGet("{id:int}/timeline")]
    [ProducesResponseType(typeof(IReadOnlyCollection<OrderStatusEventResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<OrderStatusEventResponse>>> GetTimeline(int id)
    {
        var order = await _db.DeliveryOrders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
        if (order is null)
        {
            return NotFound();
        }

        if (User.IsInRole("Courier") && order.CourierId != GetCourierId())
        {
            return Forbid();
        }

        var events = await _db.OrderStatusEvents
            .AsNoTracking()
            .Where(e => e.DeliveryOrderId == id)
            .OrderBy(e => e.ChangedAt)
            .Select(e => new OrderStatusEventResponse(e.FromStatus, e.ToStatus, e.ChangedAt, e.ChangedByCourierId))
            .ToListAsync();

        return Ok(events);
    }

    [HttpPost("{id:int}/assign")]
    [Authorize(Roles = "Admin,Dispatcher")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OrderResponse>> Assign(int id)
    {
        var result = await _assignmentService.AssignNextAvailableCourierAsync(id);

        return result switch
        {
            AssignmentResult.Success success => Ok(await MapToResponseAsync(success.Order)),
            AssignmentResult.OrderNotFound => NotFound(),
            AssignmentResult.OrderNotPending notPending => Problem(
                title: "Order is not pending",
                detail: $"Order is currently '{notPending.CurrentStatus}' and cannot be assigned.",
                statusCode: StatusCodes.Status409Conflict),
            AssignmentResult.NoCourierAvailable => Problem(
                title: "No courier available",
                detail: "No available courier was found in this order's zone.",
                statusCode: StatusCodes.Status422UnprocessableEntity),
            AssignmentResult.ConcurrencyConflict => Problem(
                title: "Concurrency conflict",
                detail: "The order was being assigned concurrently; please retry.",
                statusCode: StatusCodes.Status409Conflict),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError),
        };
    }

    [HttpPost("{id:int}/transition")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderResponse>> Transition(int id, TransitionRequest request)
    {
        int? actingCourierId = User.IsInRole("Courier") ? GetCourierId() : null;
        var result = await _transitionService.TransitionAsync(id, request.ToStatus, actingCourierId);
        return await MapTransitionResultAsync(result);
    }

    [HttpPut("{id:int}/deliver")]
    [Authorize(Roles = "Courier")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderResponse>> Deliver(int id, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Problem(
                title: "Missing Idempotency-Key header",
                detail: "The deliver endpoint requires an Idempotency-Key header so retried requests are safe.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var result = await _transitionService.DeliverAsync(id, GetCourierId(), idempotencyKey);
        return await MapTransitionResultAsync(result);
    }

    private async Task<ActionResult<OrderResponse>> MapTransitionResultAsync(OrderTransitionResult result) => result switch
    {
        OrderTransitionResult.Success success => Ok(await MapToResponseAsync(success.Order)),
        OrderTransitionResult.OrderNotFound => NotFound(),
        OrderTransitionResult.Forbidden => Forbid(),
        OrderTransitionResult.InvalidTransition invalid => Problem(
            title: "Invalid order transition",
            detail: $"Cannot move from '{invalid.CurrentStatus}' to the requested status. Allowed next states: {string.Join(", ", invalid.AllowedNextStates)}.",
            statusCode: StatusCodes.Status409Conflict),
        OrderTransitionResult.ConcurrencyConflict => Problem(
            title: "Concurrency conflict",
            detail: "The order was modified concurrently; please retry.",
            statusCode: StatusCodes.Status409Conflict),
        _ => Problem(statusCode: StatusCodes.Status500InternalServerError),
    };

    private async Task<OrderResponse> MapToResponseAsync(DeliveryOrder order)
    {
        string? courierName = order.CourierId is null
            ? null
            : await _db.Couriers.Where(c => c.Id == order.CourierId).Select(c => c.FullName).FirstOrDefaultAsync();

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        return new OrderResponse(
            order.Id, order.TrackingCode, order.PickupAddress, order.DropoffAddress, order.Zone, order.PackageWeightKg,
            order.PromisedWindowStart, order.PromisedWindowEnd, order.Status, order.CourierId, courierName,
            order.IsOverdue(now), order.Notes, order.CreatedAt, order.AssignedAt, order.PickedUpAt, order.DeliveredAt);
    }

    private int GetCourierId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
