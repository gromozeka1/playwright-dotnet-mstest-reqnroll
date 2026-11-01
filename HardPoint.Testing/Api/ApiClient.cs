using System.Text.Json;
using HardPoint.Testing.Driver;
using HardPoint.Testing.Helpers;
using Microsoft.Playwright;

namespace HardPoint.Testing.Api;

/// <summary>
/// JSON conveniences over the scenario's <see cref="IAPIRequestContext"/>. On a non-success status
/// the request and response bodies are attached to the report, which is what makes an API failure
/// readable from <c>TestReports.html</c> instead of only from a status assertion.
/// </summary>
/// <param name="playwrightDriver">The scenario's driver, resolved by Reqnroll's DI container.</param>
/// <param name="extentReportManager">The scenario's report, used for failure details.</param>
public sealed class ApiClient(PlaywrightDriver playwrightDriver, ExtentReportManager extentReportManager)
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>POSTs a JSON body and returns the response, without asserting the status.</summary>
    /// <param name="url">Absolute or context-relative URL.</param>
    /// <param name="body">Object serialised as the JSON request body.</param>
    public Task<IAPIResponse> PostJsonAsync(string url, object? body)
        => SendJsonAsync(HttpMethod.Post, url, body);

    /// <summary>PUTs a JSON body and returns the response, without asserting the status.</summary>
    /// <param name="url">Absolute or context-relative URL.</param>
    /// <param name="body">Object serialised as the JSON request body.</param>
    public Task<IAPIResponse> PutJsonAsync(string url, object? body)
        => SendJsonAsync(HttpMethod.Put, url, body);

    /// <summary>
    /// GETs a URL and deserialises its JSON body. A non-success status attaches both bodies to the
    /// report and throws with the status and the response body in the message.
    /// </summary>
    /// <typeparam name="T">The type to deserialise into.</typeparam>
    /// <param name="url">Absolute or context-relative URL.</param>
    public async Task<T> GetJsonAsync<T>(string url)
    {
        var response = await (await playwrightDriver.ApiRequestContext).GetAsync(url);
        var responseBody = await response.TextAsync();

        if (!response.Ok)
        {
            extentReportManager.AttachNote("API", Describe(HttpMethod.Get, url, response, null, responseBody));

            throw new InvalidOperationException(
                $"GET {url} returned {response.Status} {response.StatusText}: {responseBody}");
        }

        return JsonSerializer.Deserialize<T>(responseBody, _jsonOptions)
            ?? throw new InvalidOperationException($"GET {url} returned an empty JSON body");
    }

    private async Task<IAPIResponse> SendJsonAsync(HttpMethod method, string url, object? body)
    {
        var options = new APIRequestContextOptions { DataObject = body };
        var context = await playwrightDriver.ApiRequestContext;

        IAPIResponse response = method == HttpMethod.Post
            ? await context.PostAsync(url, options)
            : await context.PutAsync(url, options);

        if (!response.Ok)
        {
            extentReportManager.AttachNote(
                "API",
                Describe(method, url, response, body, await response.TextAsync()));
        }

        return response;
    }

    private static string Describe(HttpMethod method, string url, IAPIResponse response, object? requestBody, string responseBody)
    {
        var request = requestBody is null ? "no body" : JsonSerializer.Serialize(requestBody);

        return $"{method} {url} -> {response.Status} {response.StatusText}; request: {request}; response: {responseBody}";
    }
}
