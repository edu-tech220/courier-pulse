using System.Text.Json;
using System.Text.Json.Serialization;

namespace CourierPulse.Api.Tests.Infrastructure;

/// <summary>
/// Thin wrappers around System.Net.Http.Json that default to the same JsonStringEnumConverter
/// the API itself uses (see Program.cs AddJsonOptions), so test HTTP calls can (de)serialize
/// enum properties like DeliveryOrderStatus without every call site passing options explicitly.
/// Defined in this namespace (rather than importing System.Net.Http.Json) so plain
/// `client.PostAsJsonAsync(...)` calls in test files resolve here automatically.
/// </summary>
public static class JsonHttpExtensions
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static Task<HttpResponseMessage> PostAsJsonAsync<TValue>(this HttpClient client, string requestUri, TValue value) =>
        System.Net.Http.Json.HttpClientJsonExtensions.PostAsJsonAsync(client, requestUri, value, Options);

    public static Task<HttpResponseMessage> PatchAsJsonAsync<TValue>(this HttpClient client, string requestUri, TValue value) =>
        System.Net.Http.Json.HttpClientJsonExtensions.PatchAsJsonAsync(client, requestUri, value, Options);

    public static Task<T?> ReadFromJsonAsync<T>(this HttpContent content) =>
        System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync<T>(content, Options);
}
