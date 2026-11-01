using AventStack.ExtentReports;
using AventStack.ExtentReports.Reporter;
using HardPoint.Testing.Config;
using HardPoint.Testing.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HardPoint.Testing.Tests;

// The report manager reads the process-wide ConfigReader.Settings for the artifact path.
[TestClass]
[DoNotParallelize]
public sealed class ExtentReportManagerTests
{
    private string _outputDirectory = string.Empty;

    [TestInitialize]
    public void SetUp()
    {
        _outputDirectory = Path.Combine(Path.GetTempPath(), "extent-tests", Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(_outputDirectory);
        ConfigReader.Settings = new TestSettings
        {
            BrowserType = "chromium",
            ApplicationUrl = "http://localhost:5168",
            OutputDirectory = _outputDirectory,
        };
    }

    [TestCleanup]
    public void TearDown()
    {
        ConfigReader.Reload();

        if (Directory.Exists(_outputDirectory))
        {
            Directory.Delete(_outputDirectory, recursive: true);
        }
    }

    [TestMethod]
    public void AttachArtifactBeforeAScenarioStartsIsIgnored()
    {
        var manager = new ExtentReportManager();

        // Nothing has created a scenario node yet, so the call must be a no-op: an exception here
        // fails the test, which is the point.
        manager.AttachArtifact("Trace", Path.Combine(_outputDirectory, "trace.zip"));

        manager.Dispose();
    }

    [TestMethod]
    public void FeatureNodesAreCachedByTitle()
    {
        var extent = new ExtentReports();
        extent.AttachReporter(new ExtentSparkReporter(Path.Combine(_outputDirectory, "cache.html")));
        var title = $"Feature-{Guid.NewGuid():N}";

        var first = ExtentReportManager.GetOrCreateFeature(extent, title);
        var second = ExtentReportManager.GetOrCreateFeature(extent, title);
        var other = ExtentReportManager.GetOrCreateFeature(extent, $"Feature-{Guid.NewGuid():N}");

        Assert.AreSame(first, second, "the same feature title must reuse one report block");
        Assert.AreNotSame(first, other);
    }
}
