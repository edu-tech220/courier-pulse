using CourierPulse.Web.ApiClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CourierPulse.Web.Pages;

[AllowAnonymous]
public class TrackModel : PageModel
{
    private readonly CourierPulseApiClient _apiClient;

    public TrackModel(CourierPulseApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public string? TrackingCode { get; set; }

    public PublicTrackingResponse? Tracking { get; set; }

    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
    }

    public async Task OnGetSearchAsync(string trackingCode)
    {
        TrackingCode = trackingCode;
        var result = await _apiClient.GetPublicTrackingAsync(trackingCode);
        if (result.IsSuccess)
        {
            Tracking = result.Data;
        }
        else
        {
            ErrorMessage = result.StatusCode == 404
                ? "No order was found with that tracking code."
                : (result.Error ?? "Could not look up this tracking code.");
        }
    }
}
