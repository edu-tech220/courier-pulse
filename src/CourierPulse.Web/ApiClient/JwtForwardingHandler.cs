using System.Net.Http.Headers;

namespace CourierPulse.Web.ApiClient;

/// <summary>
/// Attaches the JWT stashed in the signed-in user's auth cookie (see AuthConstants.JwtClaimType)
/// as a Bearer token on every outgoing call to the API, so Razor Pages code never has to pass
/// tokens around manually.
/// </summary>
public class JwtForwardingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public JwtForwardingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = _httpContextAccessor.HttpContext?.User.FindFirst(AuthConstants.JwtClaimType)?.Value;
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
