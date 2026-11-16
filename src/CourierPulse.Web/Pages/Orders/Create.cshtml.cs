using System.ComponentModel.DataAnnotations;
using CourierPulse.Web.ApiClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CourierPulse.Web.Pages.Orders;

[Authorize(Roles = "Admin,Dispatcher")]
public class CreateModel : PageModel
{
    private readonly CourierPulseApiClient _apiClient;

    public CreateModel(CourierPulseApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _apiClient.CreateOrderAsync(new CreateOrderRequest(
            Input.PickupAddress,
            Input.DropoffAddress,
            Input.Zone,
            Input.PackageWeightKg,
            Input.PromisedWindowStart,
            Input.PromisedWindowEnd,
            Input.Notes));

        if (!result.IsSuccess || result.Data is null)
        {
            ErrorMessage = result.Error ?? "Could not create the order.";
            return Page();
        }

        return RedirectToPage("/Orders/Details", new { id = result.Data.Id });
    }

    public class InputModel
    {
        [Required, StringLength(300, MinimumLength = 3)]
        public string PickupAddress { get; set; } = string.Empty;

        [Required, StringLength(300, MinimumLength = 3)]
        public string DropoffAddress { get; set; } = string.Empty;

        [Required]
        public string Zone { get; set; } = string.Empty;

        [Range(0.01, 1000)]
        public decimal PackageWeightKg { get; set; } = 1;

        [Required]
        public DateTime PromisedWindowStart { get; set; } = DateTime.Now;

        [Required]
        public DateTime PromisedWindowEnd { get; set; } = DateTime.Now.AddHours(3);

        public string? Notes { get; set; }
    }
}
