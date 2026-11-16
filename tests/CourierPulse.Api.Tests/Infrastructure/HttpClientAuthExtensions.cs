using System.Net.Http.Headers;
using System.Net.Http.Json;
using CourierPulse.Api.Data;
using CourierPulse.Api.Dtos;

namespace CourierPulse.Api.Tests.Infrastructure;

public static class HttpClientAuthExtensions
{
    public static Task<HttpClient> AsAdminAsync(this HttpClient client) =>
        client.AuthenticateAsync(DbSeeder.AdminEmail, DbSeeder.AdminPassword);

    public static Task<HttpClient> AsDispatcherAsync(this HttpClient client) =>
        client.AuthenticateAsync("dispatcher@courierpulse.dev", "Dispatcher#12345");

    public static Task<HttpClient> AsDowntownCourierAsync(this HttpClient client) =>
        client.AuthenticateAsync("courier.downtown@courierpulse.dev", "Courier#12345");

    public static Task<HttpClient> AsNorthCourierAsync(this HttpClient client) =>
        client.AuthenticateAsync("courier.north@courierpulse.dev", "Courier#12345");

    public static async Task<HttpClient> AuthenticateAsync(this HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }
}
