using System.ComponentModel.DataAnnotations;
using CourierPulse.Api.Models;

namespace CourierPulse.Api.Dtos;

public record CreateCourierRequest(
    [property: Required, StringLength(200, MinimumLength = 2)] string FullName,
    [property: Required, EmailAddress] string Email,
    [property: Required, StringLength(100, MinimumLength = 8)] string Password,
    [property: Required] CourierRole Role,
    string? Zone);

public record CourierResponse(
    int Id,
    string FullName,
    string Email,
    CourierRole Role,
    string Zone,
    CourierAvailability Availability);

public record UpdateAvailabilityRequest([property: Required] CourierAvailability Availability);
