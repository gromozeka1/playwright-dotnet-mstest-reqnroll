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

    /// <summary>
    /// Passed to <c>IBrowser.NewContextAsync</c>: storage state, extra headers, viewport, locale,
    /// time zone, permissions, base URL and the rest of the context surface the package does not
    /// own. The HAR path and filter stay with <see cref="HarOptions"/>.
    /// </summary>
    [JsonPropertyName("browserContextOptions")]
    public BrowserNewContextOptions BrowserContextOptions { get; set; } = new();

    [JsonPropertyName("apiOptions")]
    public APIRequestNewContextOptions APIRequestNewContextOptions { get; set; } = new();

    /// <summary>
    /// The HAR archive recorded per browser context, and the URL filter applied to it.
    /// </summary>
    [JsonPropertyName("harOptions")]
    public HarOptions HarOptions { get; set; } = new();

    /// <summary>
    /// Rejects scenarios that are missing an environment tag or a test-type tag before they run.
    /// Off by default so the package does not impose a tagging policy on consumers.
    /// </summary>
    [JsonPropertyName("enforceScenarioTags")]
    public bool EnforceScenarioTags { get; set; }

    /// <summary>
    /// Writes a one-time warning when the consuming test assembly declares no
    /// <c>[assembly: Parallelize]</c> attribute, so a suite that silently runs serially is
    /// noticed. On by default; set it to <see langword="false"/> for deliberately serial suites.
    /// </summary>
    [JsonPropertyName("warnOnSerialExecution")]
    public bool WarnOnSerialExecution { get; set; } = true;

    /// <summary>
    /// Directory for the Extent report, traces, screenshots and HAR files. Empty writes them
    /// to the test output directory; a relative value is resolved against it.
    /// </summary>
    [JsonPropertyName("outputDirectory")]
    public string OutputDirectory { get; set; } = string.Empty;
}
