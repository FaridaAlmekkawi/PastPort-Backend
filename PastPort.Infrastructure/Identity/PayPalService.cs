using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using PastPort.Domain.Interfaces;
using PastPort.Application.Interfaces;

public class PayPalService : IPayPalService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    public PayPalService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _config = config;

        var baseUrl = _config["PayPal:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new ArgumentException("PayPal:BaseUrl configuration is missing or empty.");
        }

        _http.BaseAddress = new Uri(baseUrl);
    }

    private async Task<string> GetAccessToken()
    {
        var clientId = _config["PayPal:ClientId"];
        var secret = _config["PayPal:ClientSecret"];
        var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{secret}"));

        var request = new HttpRequestMessage(HttpMethod.Post, "/v1/oauth2/token");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
       {
           { "grant_type", "client_credentials" }
       });

        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        if (doc.RootElement.TryGetProperty("access_token", out var accessToken) && accessToken.GetString() is string token)
        {
            return token;
        }

        throw new InvalidOperationException("The 'access_token' property is missing or null.");
    }

    public async Task<string> CreateOrder(decimal amount)
    {
        var token = await GetAccessToken();

        var order = new
        {
            intent = "CAPTURE",
            purchase_units = new[]
            {
                new { amount = new { currency_code = "USD", value = amount.ToString("F2") } }
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/v2/checkout/orders");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent(JsonSerializer.Serialize(order), Encoding.UTF8, "application/json");

        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("id").GetString() ?? throw new InvalidOperationException("The 'id' property is missing or null.");
    }

    public async Task<string> CaptureOrder(string orderId)
    {
        var token = await GetAccessToken();

        var request = new HttpRequestMessage(HttpMethod.Post, $"/v2/checkout/orders/{orderId}/capture");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }
}
