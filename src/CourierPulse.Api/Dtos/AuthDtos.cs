using System.ComponentModel.DataAnnotations;

namespace CourierPulse.Api.Dtos;

public record LoginRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required] string Password);

public record LoginResponse(string Token, DateTime ExpiresAtUtc, string FullName, string Role);
