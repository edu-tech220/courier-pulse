using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CourierPulse.Api.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CourierPulse.Api.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;
    private readonly TimeProvider _timeProvider;

    public JwtTokenService(IOptions<JwtOptions> options, TimeProvider timeProvider)
    {
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public (string Token, DateTime ExpiresAtUtc) CreateToken(Courier courier)
    {
        var now = _timeProvider.GetUtcNow();
        var expiresAt = now.AddMinutes(_options.ExpiryMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, courier.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, courier.Id.ToString()),
            new Claim(ClaimTypes.Email, courier.Email),
            new Claim(ClaimTypes.Name, courier.FullName),
            new Claim(ClaimTypes.Role, courier.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt.UtcDateTime);
    }
}
