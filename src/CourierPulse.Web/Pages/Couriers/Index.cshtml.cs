using System.ComponentModel.DataAnnotations;
using CourierPulse.Web.ApiClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CourierPulse.Web.Pages.Couriers;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly CourierPulseApiClient _apiClient;

    public IndexModel(CourierPulseApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IReadOnlyCollection<CourierResponse> Couriers { get; set; } = Array.Empty<CourierResponse>();

    public string? ErrorMessage { get; set; }

    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        StatusMessage = TempData["StatusMessage"] as string;
        await LoadCouriersAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadCouriersAsync();
            return Page();
        }

        var result = await _apiClient.CreateCourierAsync(new CreateCourierRequest(Input.FullName, Input.Email, Input.Password, Input.Role, Input.Zone));
        TempData["StatusMessage"] = result.IsSuccess
            ? $"Account created for {Input.Email}."
            : $"Could not create the account: {result.Error}";

        return RedirectToPage();
    }

    private async Task LoadCouriersAsync()
    {
        var result = await _apiClient.GetCouriersAsync();
        if (result.IsSuccess && result.Data is not null)
        {
            Couriers = result.Data;
        }
        else
        {
            ErrorMessage = result.Error ?? "Could not load couriers.";
        }
    }

    public class InputModel
    {
        [Required, StringLength(200, MinimumLength = 2)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(100, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public CourierRole Role { get; set; } = CourierRole.Courier;

        public string? Zone { get; set; }
    }
}
