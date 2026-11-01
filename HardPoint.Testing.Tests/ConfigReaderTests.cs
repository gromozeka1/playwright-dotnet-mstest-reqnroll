using HardPoint.Testing.Config;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HardPoint.Testing.Tests;

// ConfigReader caches the settings and reads process-wide environment variables, so these
// tests must not run in parallel with each other.
[TestClass]
[DoNotParallelize]
public sealed class ConfigReaderTests
{
    private const string _configFileVariable = "TEST_CONFIG_FILE";
    private const string _headlessVariable = "TEST_Headless";
    private const string _apiBaseUrlVariable = "TEST_APIRequestNewContextOptions__BaseURL";
    private const string _harRecordVariable = "TEST_HarOptions__RecordHar";
    private const string _contextLocaleVariable = "TEST_BrowserContextOptions__Locale";
    private const string _argsVariable = "TEST_Args";
    private const string _unknownVariable = "TEST_NotASetting";
    private const string _baseDirectoryConfigFile = "relative-config-test.json";

    private static readonly string[] _managedVariables =
    [
        _configFileVariable,
        _headlessVariable,
        _apiBaseUrlVariable,
        _harRecordVariable,
        _contextLocaleVariable,
        _argsVariable,
        _unknownVariable,
    ];

    private readonly Dictionary<string, string?> _originalVariables = [];
    private string _tempDirectory = string.Empty;

    [TestInitialize]
    public void SetUp()
    {
        foreach (var name in _managedVariables)
        {
            _originalVariables[name] = Environment.GetEnvironmentVariable(name);
            Environment.SetEnvironmentVariable(name, null);
        }

        _tempDirectory = Path.Combine(Path.GetTempPath(), "config-reader-tests", Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(_tempDirectory);
        ConfigReader.Reload();
    }

    [TestCleanup]
    public void TearDown()
    {
        foreach (var (name, value) in _originalVariables)
        {
            Environment.SetEnvironmentVariable(name, value);
        }

        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }

        var baseDirectoryConfigFile = Path.Combine(AppContext.BaseDirectory, _baseDirectoryConfigFile);
        if (File.Exists(baseDirectoryConfigFile))
        {
            File.Delete(baseDirectoryConfigFile);
        }

        ConfigReader.Reload();
    }

    [TestMethod]
    public void MissingDefaultConfigFileThrowsFileNotFoundWithPathAndRemedy()
    {
        // This project deliberately does not copy appsettings.json to its output directory.
        var exception = Assert.ThrowsExactly<FileNotFoundException>(() => _ = ConfigReader.Settings);

        StringAssert.Contains(exception.Message, Path.Combine(AppContext.BaseDirectory, "appsettings.json"));
        StringAssert.Contains(exception.Message, "CopyToOutputDirectory=PreserveNewest");
    }

    [TestMethod]
    public void ConfiguredFileMissingThrowsAndQuotesTheOverride()
    {
        Environment.SetEnvironmentVariable(_configFileVariable, "does-not-exist.json");
        ConfigReader.Reload();

        var exception = Assert.ThrowsExactly<FileNotFoundException>(() => _ = ConfigReader.Settings);

        StringAssert.Contains(exception.Message, Path.Combine(AppContext.BaseDirectory, "does-not-exist.json"));
        StringAssert.Contains(exception.Message, "TEST_CONFIG_FILE is set to 'does-not-exist.json'");
    }

    [TestMethod]
    public void RelativeOverrideResolvesAgainstBaseDirectoryNotWorkingDirectory()
    {
        var expectedUrl = "http://relative.example";
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, _baseDirectoryConfigFile), CreateConfig(expectedUrl));
        Environment.SetEnvironmentVariable(_configFileVariable, _baseDirectoryConfigFile);

        var originalWorkingDirectory = Environment.CurrentDirectory;

        try
        {
            // The file exists only in the output directory, never in the working directory.
            Environment.CurrentDirectory = _tempDirectory;
            ConfigReader.Reload();

            Assert.AreEqual(expectedUrl, ConfigReader.Settings.ApplicationUrl);
        }
        finally
        {
            Environment.CurrentDirectory = originalWorkingDirectory;
        }
    }

    [TestMethod]
    public void AbsoluteOverrideLoadsFileOutsideBaseDirectory()
    {
        var configPath = Path.Combine(_tempDirectory, "custom.json");
        File.WriteAllText(configPath, CreateConfig("http://absolute.example"));
        Environment.SetEnvironmentVariable(_configFileVariable, configPath);
        ConfigReader.Reload();

        Assert.AreEqual("http://absolute.example", ConfigReader.Settings.ApplicationUrl);
    }

    [TestMethod]
    public void LeafNameOverrideAppliesToNestedSetting()
    {
        ConfigureTempConfigFile();
        Environment.SetEnvironmentVariable(_headlessVariable, "true");
        ConfigReader.Reload();

        Assert.AreEqual(true, ConfigReader.Settings.BrowserTypeLaunchOptions.Headless);
    }

    [TestMethod]
    public void PathOverrideAppliesToNestedSetting()
    {
        ConfigureTempConfigFile();
        Environment.SetEnvironmentVariable(_apiBaseUrlVariable, "http://override.example/api/");
        ConfigReader.Reload();

        Assert.AreEqual("http://override.example/api/", ConfigReader.Settings.APIRequestNewContextOptions.BaseURL);
    }

    [TestMethod]
    public void CollectionOverrideIsSplitOnCommas()
    {
        ConfigureTempConfigFile();
        Environment.SetEnvironmentVariable(_argsVariable, "--no-sandbox,--disable-dev-shm-usage");
        ConfigReader.Reload();

        string[] expected = ["--no-sandbox", "--disable-dev-shm-usage"];
        CollectionAssert.AreEqual(expected, ConfigReader.Settings.BrowserTypeLaunchOptions.Args!.ToList());
    }

    [TestMethod]
    public void UnknownOverrideIsIgnored()
    {
        ConfigureTempConfigFile();
        Environment.SetEnvironmentVariable(_unknownVariable, "1");
        ConfigReader.Reload();

        Assert.AreEqual("http://localhost:5168", ConfigReader.Settings.ApplicationUrl);
    }

    [TestMethod]
    public void HarOptionsDefaultToRecordingEveryRequest()
    {
        // The config file predates harOptions, so the section is absent and the defaults apply.
        ConfigureTempConfigFile();

        Assert.IsTrue(ConfigReader.Settings.HarOptions.RecordHar);
        Assert.AreEqual("**/*", ConfigReader.Settings.HarOptions.UrlFilter);
    }

    [TestMethod]
    public void HarOptionsAreReadFromTheConfigFile()
    {
        ConfigureTempConfigFile("har.json", CreateHarConfig(recordHar: false, urlFilter: "**/api/**"));

        Assert.IsFalse(ConfigReader.Settings.HarOptions.RecordHar);
        Assert.AreEqual("**/api/**", ConfigReader.Settings.HarOptions.UrlFilter);
    }

    [TestMethod]
    public void HarOptionOverrideAppliesToNestedSetting()
    {
        ConfigureTempConfigFile("har-override.json", CreateHarConfig());
        Environment.SetEnvironmentVariable(_harRecordVariable, "false");
        ConfigReader.Reload();

        Assert.IsFalse(ConfigReader.Settings.HarOptions.RecordHar);
    }

    [TestMethod]
    public void BrowserContextOptionsAreReadFromTheConfigFile()
    {
        ConfigureTempConfigFile("context.json", CreateContextConfig());

        var options = ConfigReader.Settings.BrowserContextOptions;

        Assert.AreEqual("auth/state.json", options.StorageStatePath);
        Assert.AreEqual("http://localhost:5168", options.BaseURL);
        Assert.AreEqual("en-GB", options.Locale);
        Assert.AreEqual("Europe/Warsaw", options.TimezoneId);
        Assert.AreEqual(true, options.IgnoreHTTPSErrors);
    }

    [TestMethod]
    public void BrowserContextOptionsDefaultToAnEmptyConfiguration()
    {
        ConfigureTempConfigFile();

        Assert.IsNull(ConfigReader.Settings.BrowserContextOptions.Locale);
        Assert.IsNull(ConfigReader.Settings.BrowserContextOptions.StorageStatePath);
    }

    [TestMethod]
    public void BrowserContextOptionOverrideAppliesToNestedSetting()
    {
        ConfigureTempConfigFile("context-override.json", CreateContextConfig());
        Environment.SetEnvironmentVariable(_contextLocaleVariable, "de-DE");
        ConfigReader.Reload();

        Assert.AreEqual("de-DE", ConfigReader.Settings.BrowserContextOptions.Locale);
    }

    [TestMethod]
    public void MalformedJsonThrowsWithPathInMessage()
    {
        var configPath = Path.Combine(_tempDirectory, "broken.json");
        File.WriteAllText(configPath, """{ "browserType": """);
        Environment.SetEnvironmentVariable(_configFileVariable, configPath);
        ConfigReader.Reload();

        var exception = Assert.ThrowsExactly<InvalidOperationException>(() => _ = ConfigReader.Settings);

        StringAssert.Contains(exception.Message, configPath);
        StringAssert.Contains(exception.Message, "is invalid");
    }

    [TestMethod]
    [DataRow("chromium")]
    [DataRow("firefox")]
    [DataRow("webkit")]
    public void DocumentedBrowserTypesAreAccepted(string browserType)
    {
        ConfigureTempConfigFile("browser-type.json", CreateConfig(browserType: browserType));

        Assert.AreEqual(browserType, ConfigReader.Settings.BrowserType);
    }

    [TestMethod]
    public void BrowserTypeIsMatchedCaseInsensitivelyAndStoredInCanonicalSpelling()
    {
        ConfigureTempConfigFile("browser-type.json", CreateConfig(browserType: "Chromium"));

        Assert.AreEqual("chromium", ConfigReader.Settings.BrowserType);
    }

    [TestMethod]
    public void UnknownBrowserTypeFailsWhenTheFileIsReadWithPathKeyAndAcceptedValues()
    {
        var configPath = ConfigureTempConfigFile("browser-type.json", CreateConfig(browserType: "chrome"));

        // Reading the configuration is enough: no browser launch, and no scenario, is needed to
        // catch the typo.
        var exception = Assert.ThrowsExactly<InvalidOperationException>(() => _ = ConfigReader.Settings);

        StringAssert.Contains(exception.Message, configPath);
        StringAssert.Contains(exception.Message, "'browserType'");
        StringAssert.Contains(exception.Message, "'chromium', 'firefox', 'webkit'");
        StringAssert.Contains(exception.Message, "but was 'chrome'");
    }

    private string ConfigureTempConfigFile(string fileName = "config.json", string? content = null)
    {
        var configPath = Path.Combine(_tempDirectory, fileName);
        File.WriteAllText(configPath, content ?? CreateConfig());
        Environment.SetEnvironmentVariable(_configFileVariable, configPath);

        return configPath;
    }

    private static string CreateContextConfig() => """
        {
          "browserType": "chromium",
          "applicationUrl": "http://localhost:5168",
          "browserContextOptions": {
            "storageStatePath": "auth/state.json",
            "baseURL": "http://localhost:5168",
            "locale": "en-GB",
            "timezoneId": "Europe/Warsaw",
            "ignoreHTTPSErrors": true
          }
        }
        """;

    private static string CreateHarConfig(bool recordHar = true, string urlFilter = "**/*") => $$"""
        {
          "browserType": "chromium",
          "applicationUrl": "http://localhost:5168",
          "harOptions": {
            "recordHar": {{(recordHar ? "true" : "false")}},
            "urlFilter": "{{urlFilter}}"
          }
        }
        """;

    private static string CreateConfig(string applicationUrl = "http://localhost:5168", string browserType = "chromium") => $$"""
        {
          "browserType": "{{browserType}}",
          "applicationUrl": "{{applicationUrl}}",
          "browserOptions": {
            "args": null,
            "headless": false,
            "timeout": 30000
          },
          "apiOptions": {
            "BaseURL": "http://localhost:5044/api/",
            "IgnoreHTTPSErrors": true
          }
        }
        """;
}
