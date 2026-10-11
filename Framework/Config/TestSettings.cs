namespace Framework.Config;

public class TestSettings
{
    public float? Timeout { get; set; }
    public DriverType DriverType { get; set; }
    public required string ApplicationUrl { get; set; }
    public string? ApplicationApiUrl { get; set; }
    public string[]? Args { get; set; }
    public bool? Headless { get; set; }
    public float? SlowMo { get; set; }
}
