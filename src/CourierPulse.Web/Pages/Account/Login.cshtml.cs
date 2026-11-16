using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using CourierPulse.Web.ApiClient;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CourierPulse.Web.Pages.Account;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly CourierPulseApiClient _apiClient;

    public LoginModel(CourierPulseApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _apiClient.LoginAsync(Input.Email, Input.Password);
        if (!result.IsSuccess || result.Data is null)
        {
            ErrorMessage = "Invalid email or password.";
            return Page();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, result.Data.FullName),
            new(ClaimTypes.Email, Input.Email),
            new(ClaimTypes.Role, result.Data.Role),
            new(AuthConstants.JwtClaimType, result.Data.Token),
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return LocalRedirect(HomePathForRole(result.Data.Role));
    }

    private static string HomePathForRole(string role) => role switch
    {
        "Admin" => "/Couriers",
        "Dispatcher" => "/Dashboard",
        "Courier" => "/MyDeliveries",
        _ => "/",
    };

    public class InputModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
