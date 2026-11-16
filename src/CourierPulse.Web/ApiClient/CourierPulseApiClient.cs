using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CourierPulse.Web.ApiClient;

/// <summary>Typed HttpClient wrapping every call the frontend makes to the CourierPulse.Api microservice.</summary>
public class CourierPulseApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly HttpClient _httpClient;

    public CourierPulseApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<ApiResult<LoginResponse>> LoginAsync(string email, string password) =>
        SendAsync<LoginResponse>(() => _httpClient.PostAsJsonAsync("api/auth/login", new LoginRequest(email, password), JsonOptions));

    public Task<ApiResult<PagedResult<OrderResponse>>> GetOrdersAsync(
        DeliveryOrderStatus? status, string? zone, bool overdueOnly, int page, int pageSize = 20)
    {
        var query = BuildOrdersQuery(status, zone, overdueOnly, page, pageSize);
        return SendAsync<PagedResult<OrderResponse>>(() => _httpClient.GetAsync($"api/orders{query}"));
    }

    public Task<ApiResult<PagedResult<OrderResponse>>> GetMyOrdersAsync(int page, int pageSize = 20) =>
        SendAsync<PagedResult<OrderResponse>>(() => _httpClient.GetAsync($"api/orders/mine?page={page}&pageSize={pageSize}"));

    public Task<ApiResult<OrderResponse>> GetOrderAsync(int id) =>
        SendAsync<OrderResponse>(() => _httpClient.GetAsync($"api/orders/{id}"));

    public Task<ApiResult<List<OrderStatusEventResponse>>> GetOrderTimelineAsync(int id) =>
        SendAsync<List<OrderStatusEventResponse>>(() => _httpClient.GetAsync($"api/orders/{id}/timeline"));

    public Task<ApiResult<OrderResponse>> CreateOrderAsync(CreateOrderRequest request) =>
        SendAsync<OrderResponse>(() => _httpClient.PostAsJsonAsync("api/orders", request, JsonOptions));

    public Task<ApiResult<OrderResponse>> AssignOrderAsync(int id) =>
        SendAsync<OrderResponse>(() => _httpClient.PostAsync($"api/orders/{id}/assign", content: null));

    public Task<ApiResult<OrderResponse>> TransitionOrderAsync(int id, DeliveryOrderStatus toStatus) =>
        SendAsync<OrderResponse>(() => _httpClient.PostAsJsonAsync($"api/orders/{id}/transition", new TransitionRequest(toStatus), JsonOptions));

    public Task<ApiResult<OrderResponse>> DeliverOrderAsync(int id, string idempotencyKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"api/orders/{id}/deliver");
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return SendAsync<OrderResponse>(() => _httpClient.SendAsync(request));
    }

    public Task<ApiResult<List<CourierResponse>>> GetCouriersAsync() =>
        SendAsync<List<CourierResponse>>(() => _httpClient.GetAsync("api/couriers"));

    public Task<ApiResult<CourierResponse>> CreateCourierAsync(CreateCourierRequest request) =>
        SendAsync<CourierResponse>(() => _httpClient.PostAsJsonAsync("api/couriers", request, JsonOptions));

    public Task<ApiResult<CourierResponse>> UpdateMyAvailabilityAsync(CourierAvailability availability) =>
        SendAsync<CourierResponse>(() => _httpClient.PatchAsJsonAsync("api/couriers/me/availability", new UpdateAvailabilityRequest(availability), JsonOptions));

    public Task<ApiResult<PublicTrackingResponse>> GetPublicTrackingAsync(string trackingCode) =>
        SendAsync<PublicTrackingResponse>(() => _httpClient.GetAsync($"api/public/tracking/{Uri.EscapeDataString(trackingCode)}"));

    private static string BuildOrdersQuery(DeliveryOrderStatus? status, string? zone, bool overdueOnly, int page, int pageSize)
    {
        var parameters = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (status is not null)
        {
            parameters.Add($"status={status}");
        }

        if (!string.IsNullOrWhiteSpace(zone))
        {
            parameters.Add($"zone={Uri.EscapeDataString(zone)}");
        }

        if (overdueOnly)
        {
            parameters.Add("overdueOnly=true");
        }

        return "?" + string.Join('&', parameters);
    }

    private static async Task<ApiResult<T>> SendAsync<T>(Func<Task<HttpResponseMessage>> send)
    {
        using var response = await send();
        var statusCode = (int)response.StatusCode;

        if (!response.IsSuccessStatusCode)
        {
            return ApiResult<T>.Fail(statusCode, await ExtractErrorAsync(response));
        }

        var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        return ApiResult<T>.Ok(statusCode, data!);
    }

    private static async Task<string?> ExtractErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsDto>(JsonOptions);
            return problem?.Detail ?? problem?.Title;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private record ProblemDetailsDto(string? Title, string? Detail);
}
