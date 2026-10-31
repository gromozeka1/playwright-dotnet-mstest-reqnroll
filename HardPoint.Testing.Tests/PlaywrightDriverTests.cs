using System.Text.RegularExpressions;
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

    [TestMethod]
    public void ConsumerBrowserContextOptionsArePassedThrough()
    {
        var storageStatePath = Path.Combine(_outputDirectory, "auth", "state.json");

        BrowserNewContextOptions options = BuildContextOptions(
            new HarOptions(),
            new BrowserNewContextOptions
            {
                StorageStatePath = storageStatePath,
                BaseURL = "http://localhost:5168",
                Locale = "en-GB",
                TimezoneId = "Europe/Warsaw",
                IgnoreHTTPSErrors = true,
                ExtraHTTPHeaders = new Dictionary<string, string> { ["x-api-key"] = "secret" },
                ViewportSize = new ViewportSize { Width = 1440, Height = 900 },
                Permissions = ["geolocation"],
            });

        Assert.AreEqual(storageStatePath, options.StorageStatePath);
        Assert.AreEqual("http://localhost:5168", options.BaseURL);
        Assert.AreEqual("en-GB", options.Locale);
        Assert.AreEqual("Europe/Warsaw", options.TimezoneId);
        Assert.AreEqual(true, options.IgnoreHTTPSErrors);
        Assert.AreEqual("secret", options.ExtraHTTPHeaders!.Single().Value);
        Assert.AreEqual(1440, options.ViewportSize!.Width);
        Assert.AreEqual(900, options.ViewportSize.Height);
        Assert.AreEqual("geolocation", options.Permissions!.Single());
    }

    [TestMethod]
    public void FrameworkWinsOnTheHarSettings()
    {
        var consumerHarPath = Path.Combine(_outputDirectory, "consumer.har");

        BrowserNewContextOptions options = BuildContextOptions(
            new HarOptions { UrlFilter = "**/api/**" },
            new BrowserNewContextOptions
            {
                RecordHarPath = consumerHarPath,
                RecordHarUrlFilter = "**/consumer/**",
                RecordHarUrlFilterRegex = new Regex("consumer"),
            });

        AssertHarPathIsRoutedToArtifacts(options.RecordHarPath);
        Assert.AreNotEqual(consumerHarPath, options.RecordHarPath);
        Assert.AreEqual("**/api/**", options.RecordHarUrlFilterString);
        Assert.IsNull(options.RecordHarUrlFilter);
        Assert.IsNull(options.RecordHarUrlFilterRegex);
    }

    [TestMethod]
    public void RecordHarFalseLeavesTheConsumersOwnHarConfigurationAlone()
    {
        var consumerHarPath = Path.Combine(_outputDirectory, "consumer.har");

        BrowserNewContextOptions options = BuildContextOptions(
            new HarOptions { RecordHar = false },
            new BrowserNewContextOptions { RecordHarPath = consumerHarPath });

        Assert.AreEqual(consumerHarPath, options.RecordHarPath);
    }

    [TestMethod]
    public void ContextOptionsAreCopiedSoParallelScenariosDoNotShareOneInstance()
    {
        var consumerOptions = new BrowserNewContextOptions { Locale = "en-GB" };

        BrowserNewContextOptions options = BuildContextOptions(new HarOptions(), consumerOptions);

        Assert.AreNotSame(consumerOptions, options);
        options.Locale = "de-DE";
        Assert.AreEqual("en-GB", consumerOptions.Locale, "building one context must not change the settings");
    }

    private BrowserNewContextOptions BuildContextOptions(
        HarOptions harOptions,
        BrowserNewContextOptions? browserContextOptions = null)
    {
        ConfigReader.Settings = new TestSettings
        {
            BrowserType = "chromium",
            ApplicationUrl = "http://localhost:5168",
            OutputDirectory = _outputDirectory,
            BrowserContextOptions = browserContextOptions ?? new BrowserNewContextOptions(),
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
