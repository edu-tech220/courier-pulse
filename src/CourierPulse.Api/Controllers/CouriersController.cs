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
[Route("api/couriers")]
[Authorize]
public class CouriersController : ControllerBase
{
    private readonly CourierPulseDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    public CouriersController(CourierPulseDbContext db, IPasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Dispatcher")]
    [ProducesResponseType(typeof(IReadOnlyCollection<CourierResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<CourierResponse>>> GetAll()
    {
        var couriers = await _db.Couriers
            .OrderBy(c => c.FullName)
            .Select(c => new CourierResponse(c.Id, c.FullName, c.Email, c.Role, c.Zone, c.Availability))
            .ToListAsync();

        return Ok(couriers);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CourierResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CourierResponse>> Create(CreateCourierRequest request)
    {
        if (await _db.Couriers.AnyAsync(c => c.Email == request.Email))
        {
            return Problem(
                title: "Email already registered",
                detail: $"A courier account with email '{request.Email}' already exists.",
                statusCode: StatusCodes.Status409Conflict);
        }

        var courier = new Courier
        {
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            Role = request.Role,
            Zone = request.Zone ?? string.Empty,
            Availability = CourierAvailability.Offline,
            CreatedAt = DateTime.UtcNow,
        };

        _db.Couriers.Add(courier);
        await _db.SaveChangesAsync();

        var response = new CourierResponse(courier.Id, courier.FullName, courier.Email, courier.Role, courier.Zone, courier.Availability);
        return CreatedAtAction(nameof(GetAll), new { }, response);
    }

    [HttpPatch("me/availability")]
    [Authorize(Roles = "Courier")]
    [ProducesResponseType(typeof(CourierResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CourierResponse>> UpdateMyAvailability(UpdateAvailabilityRequest request)
    {
        var courierId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var courier = await _db.Couriers.FindAsync(courierId);
        if (courier is null)
        {
            return NotFound();
        }

        courier.Availability = request.Availability;
        await _db.SaveChangesAsync();

        return Ok(new CourierResponse(courier.Id, courier.FullName, courier.Email, courier.Role, courier.Zone, courier.Availability));
    }
}
