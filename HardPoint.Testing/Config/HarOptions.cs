using System.Text.Json.Serialization;

namespace HardPoint.Testing.Config;

/// <summary>
/// Controls the HAR archive the package records for every browser context.
/// </summary>
/// <remarks>
/// Nothing here is specific to one test suite: the archive is written next to the report and
/// filtered only by what the consumer asks for, so a failing CI run has the network log of the
/// application actually under test.
/// </remarks>
public sealed class HarOptions
{
    /// <summary>
    /// Record a HAR archive per browser context. Default <see langword="true"/>; set it to
    /// <see langword="false"/> to skip recording altogether.
    /// </summary>
    [JsonPropertyName("recordHar")]
    public bool RecordHar { get; set; } = true;

    /// <summary>
    /// Playwright glob or regex selecting the requests that reach the archive. Default
    /// <c>**/*</c>, which records every request; an empty value also records every request.
    /// </summary>
    [JsonPropertyName("urlFilter")]
    public string? UrlFilter { get; set; } = "**/*";
}
