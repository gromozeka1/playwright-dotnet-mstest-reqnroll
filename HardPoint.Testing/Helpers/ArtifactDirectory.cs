using HardPoint.Testing.Config;

namespace HardPoint.Testing.Helpers;

/// <summary>
/// Resolves where a run writes its artifacts: the Extent report, Playwright traces,
/// screenshots and HAR files all live under one configurable directory instead of
/// wherever the framework assembly happens to be loaded from.
/// </summary>
/// <remarks>
/// The directory is <see cref="TestSettings.OutputDirectory"/> when set, resolved
/// against the test output directory if it is relative, and the test output directory
/// itself when it is not.
/// </remarks>
public static class ArtifactDirectory
{
    /// <summary>
    /// The directory all artifacts are written to. Created on first access.
    /// </summary>
    public static string Root
    {
        get
        {
            var configured = ConfigReader.Settings.OutputDirectory;
            var root = string.IsNullOrWhiteSpace(configured)
                ? BaseDirectory
                : Path.GetFullPath(configured, BaseDirectory);

            _ = Directory.CreateDirectory(root);
            return root;
        }
    }

    /// <summary>
    /// Playwright trace archives, kept only for failing scenarios.
    /// </summary>
    public static string Traces => Path.Combine(Root, "traces");

    /// <summary>
    /// Screenshots attached to report steps and to failed scenarios.
    /// </summary>
    public static string Screenshots => Path.Combine(Root, "screenshots");

    /// <summary>
    /// The Extent HTML report.
    /// </summary>
    public static string Report => Path.Combine(Root, "TestReports.html");

    private static string BaseDirectory => AppContext.BaseDirectory;
}
