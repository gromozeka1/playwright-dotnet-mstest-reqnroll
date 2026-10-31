using HardPoint.Testing.Config;
using HardPoint.Testing.Driver;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HardPoint.Testing.Tests;

// The driver snapshots the process-wide ConfigReader.Settings when it is constructed, so these
// tests must not run in parallel with the tests that read or replace those settings.
[TestClass]
[DoNotParallelize]
public sealed class PlaywrightDriverTests
{
    private string _outputDirectory = string.Empty;

    [TestInitialize]
    public void SetUp()
    {
        _outputDirectory = Path.Combine(Path.GetTempPath(), "playwright-driver-tests", Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(_outputDirectory);
    }

    [TestCleanup]
    public void TearDown()
    {
        // Reload instead of leaving the in-memory settings behind: the ConfigReader tests assert
        // on the re-read (missing-file) behaviour.
        ConfigReader.Reload();

        if (Directory.Exists(_outputDirectory))
        {
            Directory.Delete(_outputDirectory, recursive: true);
        }
    }

    [TestMethod]
    public void HarRecordsEveryRequestByDefault()
    {
        BrowserNewContextOptions options = BuildContextOptions(new HarOptions());

        AssertHarPathIsRoutedToArtifacts(options.RecordHarPath);
        Assert.AreEqual("**/*", options.RecordHarUrlFilterString);
    }

    [TestMethod]
    public void CustomUrlFilterIsPassedToPlaywright()
    {
        BrowserNewContextOptions options = BuildContextOptions(new HarOptions { UrlFilter = "**/api/**" });

        AssertHarPathIsRoutedToArtifacts(options.RecordHarPath);
        Assert.AreEqual("**/api/**", options.RecordHarUrlFilterString);
    }

    [TestMethod]
    public void BlankUrlFilterRecordsEveryRequest()
    {
        BrowserNewContextOptions options = BuildContextOptions(new HarOptions { UrlFilter = "  " });

        AssertHarPathIsRoutedToArtifacts(options.RecordHarPath);
        Assert.IsNull(options.RecordHarUrlFilterString);
    }

    [TestMethod]
    public void RecordHarFalseWritesNoArchive()
    {
        BrowserNewContextOptions options = BuildContextOptions(new HarOptions { RecordHar = false, UrlFilter = "**/api/**" });

        Assert.IsNull(options.RecordHarPath);
        Assert.IsNull(options.RecordHarUrlFilterString);
    }

    private BrowserNewContextOptions BuildContextOptions(HarOptions harOptions)
    {
        ConfigReader.Settings = new TestSettings
        {
            BrowserType = "chromium",
            ApplicationUrl = "http://localhost:5168",
            OutputDirectory = _outputDirectory,
            HarOptions = harOptions,
        };

        // Every resource is lazy, so constructing the driver and building the options starts
        // nothing: these tests need no browser binary.
        var driver = new PlaywrightDriver();

        return driver.CreateBrowserContextOptions();
    }

    private void AssertHarPathIsRoutedToArtifacts(string? harPath)
    {
        Assert.IsNotNull(harPath);
        Assert.AreEqual(_outputDirectory, Path.GetDirectoryName(harPath), "the HAR should sit next to the other artifacts");

        var fileName = Path.GetFileName(harPath);
        StringAssert.StartsWith(fileName, "sut-");
        StringAssert.EndsWith(fileName, ".har");
    }
}
