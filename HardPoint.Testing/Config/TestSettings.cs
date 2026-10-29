using System.Text.Json.Serialization;
using Microsoft.Playwright;

namespace HardPoint.Testing.Config;

public class TestSettings
{
    [JsonPropertyName("browserType")]
    public required string BrowserType { get; set; }

    [JsonPropertyName("applicationUrl")]
    public required string ApplicationUrl { get; set; }

    [JsonPropertyName("browserOptions")]
    public BrowserTypeLaunchOptions BrowserTypeLaunchOptions { get; set; } = new();

    [JsonPropertyName("apiOptions")]
    public APIRequestNewContextOptions APIRequestNewContextOptions { get; set; } = new();
}
