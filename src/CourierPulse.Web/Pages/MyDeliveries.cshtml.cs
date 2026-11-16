using CourierPulse.Web.ApiClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CourierPulse.Web.Pages;

[Authorize(Roles = "Courier")]
public class MyDeliveriesModel : PageModel
{
    private readonly CourierPulseApiClient _apiClient;

    public MyDeliveriesModel(CourierPulseApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public PagedResult<OrderResponse>? Orders { get; set; }

    public string? ErrorMessage { get; set; }

    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        StatusMessage = TempData["StatusMessage"] as string;

        var result = await _apiClient.GetMyOrdersAsync(page: 1, pageSize: 50);
        if (result.IsSuccess)
        {
            Orders = result.Data;
        }
        else
        {
            ErrorMessage = result.Error ?? "Could not load your deliveries.";
        }
    }

    public async Task<IActionResult> OnPostAvailabilityAsync(CourierAvailability availability)
    {
        var result = await _apiClient.UpdateMyAvailabilityAsync(availability);
        TempData["StatusMessage"] = result.IsSuccess
            ? $"Availability set to {availability}."
            : $"Could not update availability: {result.Error}";

        return RedirectToPage();
    }
}
