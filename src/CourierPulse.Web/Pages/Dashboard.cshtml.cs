using CourierPulse.Web.ApiClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CourierPulse.Web.Pages;

[Authorize(Roles = "Admin,Dispatcher")]
public class DashboardModel : PageModel
{
    private readonly CourierPulseApiClient _apiClient;

    public DashboardModel(CourierPulseApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [BindProperty(SupportsGet = true)]
    public DeliveryOrderStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Zone { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool OverdueOnly { get; set; }

    [BindProperty(SupportsGet = true, Name = "page")]
    public int PageNumber { get; set; } = 1;

    public PagedResult<OrderResponse>? Orders { get; set; }

    public string? ErrorMessage { get; set; }

    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadOrdersAsync();
    }

    public async Task<IActionResult> OnPostAssignAsync(int id)
    {
        var result = await _apiClient.AssignOrderAsync(id);
        TempData["StatusMessage"] = result.IsSuccess
            ? $"Order #{id} assigned to {result.Data?.CourierName}."
            : $"Could not assign order #{id}: {result.Error ?? "no courier available."}";

        return RedirectToPage(new { Status, Zone, OverdueOnly, page = PageNumber });
    }

    private async Task LoadOrdersAsync()
    {
        StatusMessage = TempData["StatusMessage"] as string;
        var result = await _apiClient.GetOrdersAsync(Status, Zone, OverdueOnly, PageNumber);
        if (result.IsSuccess)
        {
            Orders = result.Data;
        }
        else
        {
            ErrorMessage = result.Error ?? "Could not load orders.";
        }
    }
}
