using CourierPulse.Web.ApiClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CourierPulse.Web.Pages.Orders;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly CourierPulseApiClient _apiClient;

    public DetailsModel(CourierPulseApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public OrderResponse? Order { get; set; }

    public IReadOnlyCollection<OrderStatusEventResponse> Timeline { get; set; } = Array.Empty<OrderStatusEventResponse>();

    public string? ErrorMessage { get; set; }

    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        await LoadAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostAssignAsync(int id)
    {
        var result = await _apiClient.AssignOrderAsync(id);
        TempData["StatusMessage"] = result.IsSuccess ? "Order assigned." : $"Could not assign: {result.Error}";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostTransitionAsync(int id, DeliveryOrderStatus toStatus)
    {
        var result = await _apiClient.TransitionOrderAsync(id, toStatus);
        TempData["StatusMessage"] = result.IsSuccess ? $"Order moved to {toStatus}." : $"Could not update the order: {result.Error}";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeliverAsync(int id)
    {
        var result = await _apiClient.DeliverOrderAsync(id, Guid.NewGuid().ToString());
        TempData["StatusMessage"] = result.IsSuccess ? "Order marked as delivered." : $"Could not confirm delivery: {result.Error}";
        return RedirectToPage(new { id });
    }

    private async Task LoadAsync(int id)
    {
        StatusMessage = TempData["StatusMessage"] as string;

        var orderResult = await _apiClient.GetOrderAsync(id);
        if (!orderResult.IsSuccess)
        {
            ErrorMessage = orderResult.Error;
            return;
        }

        Order = orderResult.Data;

        var timelineResult = await _apiClient.GetOrderTimelineAsync(id);
        if (timelineResult.IsSuccess && timelineResult.Data is not null)
        {
            Timeline = timelineResult.Data;
        }
    }
}
