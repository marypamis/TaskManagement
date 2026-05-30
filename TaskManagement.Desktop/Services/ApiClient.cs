using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TaskManagement.Desktop.Helpers;
using TaskManagement.Desktop.Models;

namespace TaskManagement.Desktop.Services;

// marypamis: Centralizing API calls here to simplify future endpoint changes
public class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly SessionTokenStore _tokenStore;

    public ApiClient(SessionTokenStore tokenStore)
    {
        _tokenStore = tokenStore;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(ApiSettings.BaseUrl)
        };
    }

    public async Task<(bool Success, T? Data, string? ErrorMessage)> PostAsync<T>(
        string url,
        object body,
        bool requireAuth = false,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body)
        };

        ApplyAuthHeader(request, requireAuth);

        var sendResult = await SendRequestAsync(request, cancellationToken);
        if (!sendResult.Success)
        {
            return (false, default, sendResult.ErrorMessage);
        }

        using var response = sendResult.Response!;

        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
            return (true, data, null);
        }

        return (false, default, await ReadErrorMessageAsync(response, cancellationToken));
    }

    public async Task<(bool Success, T? Data, string? ErrorMessage)> GetAsync<T>(
        string url,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        ApplyAuthHeader(request, requireAuth: true);

        var sendResult = await SendRequestAsync(request, cancellationToken);
        if (!sendResult.Success)
        {
            return (false, default, sendResult.ErrorMessage);
        }

        using var response = sendResult.Response!;

        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
            return (true, data, null);
        }

        return (false, default, await ReadErrorMessageAsync(response, cancellationToken));
    }

    public async Task<(bool Success, T? Data, string? ErrorMessage)> PatchAsync<T>(
        string url,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Patch, url);
        ApplyAuthHeader(request, requireAuth: true);

        var sendResult = await SendRequestAsync(request, cancellationToken);
        if (!sendResult.Success)
        {
            return (false, default, sendResult.ErrorMessage);
        }

        using var response = sendResult.Response!;

        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
            return (true, data, null);
        }

        return (false, default, await ReadErrorMessageAsync(response, cancellationToken));
    }

    private async Task<(bool Success, HttpResponseMessage? Response, string? ErrorMessage)> SendRequestAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);
            return (true, response, null);
        }
        catch (HttpRequestException)
        {
            // marypamis: show a clear message when the API is not running locally
            return (false, null, $"Cannot connect to API at {ApiSettings.BaseUrl}. Start TaskManagement.API first.");
        }
        catch (TaskCanceledException)
        {
            return (false, null, "Request timed out. Check that the API is running.");
        }
    }

    private void ApplyAuthHeader(HttpRequestMessage request, bool requireAuth)
    {
        if (!requireAuth || !_tokenStore.HasToken)
        {
            return;
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenStore.Token);
    }

    private static async Task<string> ReadErrorMessageAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            return "Invalid username or password.";
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            return "You do not have permission for this action.";
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(body))
        {
            return $"Request failed ({(int)response.StatusCode}).";
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("message", out var message))
            {
                return message.GetString() ?? body;
            }
        }
        catch (JsonException)
        {
            // fall through to raw body
        }

        return body;
    }
}
