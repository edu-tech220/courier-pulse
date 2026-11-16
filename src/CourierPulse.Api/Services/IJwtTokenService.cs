using CourierPulse.Api.Models;

namespace CourierPulse.Api.Services;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateToken(Courier courier);
}
