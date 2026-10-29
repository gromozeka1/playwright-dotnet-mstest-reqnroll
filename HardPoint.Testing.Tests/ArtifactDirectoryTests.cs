using HardPoint.Testing.Config;
using HardPoint.Testing.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HardPoint.Testing.Tests;

// ArtifactDirectory resolves through ConfigReader's cached settings and reads process-wide
// environment variables, so these tests cannot run in parallel with each other.
[TestClass]
[DoNotParallelize]
public sealed class ArtifactDirectoryTests
{
    private const string _configFileVariable = "TEST_CONFIG_FILE";
    private const string _outputDirectoryVariable = "TEST_OutputDirectory";
    private const string _enforceTagsVariable = "TEST_EnforceScenarioTags";

    private readonly Dictionary<string, string?> _originalVariables = [];
    private string _tempDirectory = string.Empty;

    [TestInitialize]
    public void SetUp()
    {
        foreach (var name in new[] { _configFileVariable, _outputDirectoryVariable, _enforceTagsVariable })
        {
            _originalVariables[name] = Environment.GetEnvironmentVariable(name);
            Environment.SetEnvironmentVariable(name, null);
        }

        _tempDirectory = Path.Combine(Path.GetTempPath(), "artifact-directory-tests", Guid.NewGuid().ToString("N"));
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

        ConfigReader.Reload();
    }

    [TestMethod]
    public void UnconfiguredOutputDirectoryWritesArtifactsToTheTestOutputDirectory()
    {
        ConfigureConfigFile(outputDirectory: null);

        Assert.AreEqual(AppContext.BaseDirectory, ArtifactDirectory.Root);
        Assert.AreEqual(Path.Combine(AppContext.BaseDirectory, "traces"), ArtifactDirectory.Traces);
        Assert.AreEqual(Path.Combine(AppContext.BaseDirectory, "screenshots"), ArtifactDirectory.Screenshots);
        Assert.AreEqual(Path.Combine(AppContext.BaseDirectory, "TestReports.html"), ArtifactDirectory.Report);
    }

    [TestMethod]
    public void RelativeOutputDirectoryResolvesAgainstTheTestOutputDirectory()
    {
        ConfigureConfigFile(Path.Combine("artifacts", "run"));

        Assert.AreEqual(Path.Combine(AppContext.BaseDirectory, "artifacts", "run"), ArtifactDirectory.Root);
    }

    [TestMethod]
    public void AbsoluteOutputDirectoryIsUsedAsIs()
    {
        var absoluteDirectory = Path.Combine(_tempDirectory, "artifacts");

        ConfigureConfigFile(absoluteDirectory);

        Assert.AreEqual(absoluteDirectory, ArtifactDirectory.Root);
    }

    [TestMethod]
    public void ScenarioTagEnforcementIsOffWhenTheSettingIsAbsent()
    {
        ConfigureConfigFile(outputDirectory: null);

        Assert.IsFalse(ConfigReader.Settings.EnforceScenarioTags);
    }

    [TestMethod]
    public void ScenarioTagEnforcementCanBeTurnedOnByEnvironmentOverride()
    {
        ConfigureConfigFile(outputDirectory: null, enforceScenarioTags: false);
        Environment.SetEnvironmentVariable(_enforceTagsVariable, "true");
        ConfigReader.Reload();

        Assert.IsTrue(ConfigReader.Settings.EnforceScenarioTags);
    }

    private void ConfigureConfigFile(string? outputDirectory, bool? enforceScenarioTags = null)
    {
        var outputDirectorySetting = outputDirectory is null
            ? string.Empty
            : $$"""
                "outputDirectory": {{System.Text.Json.JsonSerializer.Serialize(outputDirectory)}},
                """;

        var enforceTagsSetting = enforceScenarioTags is null
            ? string.Empty
            : $$"""
                "enforceScenarioTags": {{(enforceScenarioTags.Value ? "true" : "false")}},
                """;

        var configPath = Path.Combine(_tempDirectory, "config.json");
        File.WriteAllText(configPath, $$"""
            {
              "browserType": "chromium",
              "applicationUrl": "http://localhost:5168",
              {{outputDirectorySetting}}
              {{enforceTagsSetting}}
              "browserOptions": {
                "headless": true
              },
              "apiOptions": {
                "BaseURL": "http://localhost:5044/api/"
              }
            }
            """);
        Environment.SetEnvironmentVariable(_configFileVariable, configPath);
        ConfigReader.Reload();
    }
}
