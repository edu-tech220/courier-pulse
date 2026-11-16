using System.ComponentModel.DataAnnotations;
using CourierPulse.Api.Models;

namespace CourierPulse.Api.Dtos;

public record CreateCourierRequest(
    [Required, StringLength(200, MinimumLength = 2)] string FullName,
    [Required, EmailAddress] string Email,
    [Required, StringLength(100, MinimumLength = 8)] string Password,
    [Required] CourierRole Role,
    string? Zone);

public record CourierResponse(
    int Id,
    string FullName,
    string Email,
    CourierRole Role,
    string Zone,
    CourierAvailability Availability);

public record UpdateAvailabilityRequest([Required] CourierAvailability Availability);
