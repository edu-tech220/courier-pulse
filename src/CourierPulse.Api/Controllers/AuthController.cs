using CourierPulse.Api.Data;
using CourierPulse.Api.Dtos;
using CourierPulse.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CourierPulse.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly CourierPulseDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthController(CourierPulseDbContext db, IPasswordHasher passwordHasher, IJwtTokenService jwtTokenService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var courier = await _db.Couriers.SingleOrDefaultAsync(c => c.Email == request.Email);
        if (courier is null || !_passwordHasher.VerifyPassword(request.Password, courier.PasswordHash))
        {
            return Problem(
                title: "Invalid credentials",
                detail: "The email or password is incorrect.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var (token, expiresAtUtc) = _jwtTokenService.CreateToken(courier);
        return Ok(new LoginResponse(token, expiresAtUtc, courier.FullName, courier.Role.ToString()));
    }
}
