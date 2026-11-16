using System.ComponentModel.DataAnnotations;

namespace CourierPulse.Api.Dtos;

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public record LoginResponse(string Token, DateTime ExpiresAtUtc, string FullName, string Role);
