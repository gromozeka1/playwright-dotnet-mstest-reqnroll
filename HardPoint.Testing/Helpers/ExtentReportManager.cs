using AventStack.ExtentReports;
using AventStack.ExtentReports.Gherkin;
using AventStack.ExtentReports.Gherkin.Model;
using AventStack.ExtentReports.Model;
using AventStack.ExtentReports.Reporter;
using HardPoint.Testing.Config;
using HardPoint.Testing.Driver;
using Reqnroll;

namespace HardPoint.Testing.Helpers;

/// <summary>
/// The per-run Extent HTML report: one block per feature, one node per scenario, serialised
/// behind a single lock so parallel scenarios can share it.
/// </summary>
public class ExtentReportManager : IDisposable
{
    // The reporter and the per-feature nodes are shared by every scenario of the run,
    // and scenarios execute in parallel. One process-wide lock guards their creation:
    // locking on a new object per call (the previous behaviour) excluded nothing, and
    // creating a feature node per scenario duplicated every feature in the report.
    private static readonly Lock _reportLock = new();
    private static readonly Dictionary<string, ExtentTest> _featuresByTitle = new(StringComparer.Ordinal);
    private static ExtentReports? _extentReports;

    private ExtentTest? _scenario;
    private ExtentTest? _artifacts;
    private bool _disposedValue;

    /// <summary>Wires the shared reporter to <c>TestReports.html</c> in the artifact directory.</summary>
    public ExtentReportManager()
    {
        lock (_reportLock)
        {
            _ = GetOrCreateReporter();
        }
    }

    // Must be called while holding _reportLock.
    private static ExtentReports GetOrCreateReporter()
    {
        if (_extentReports is null)
        {
            _extentReports = new ExtentReports();
            _extentReports.AddSystemInfo("os", Environment.OSVersion.ToString());
            _extentReports.AddSystemInfo("browser", ConfigReader.Settings.BrowserType);
            _extentReports.AttachReporter(new ExtentSparkReporter(ArtifactDirectory.Report));
        }

        return _extentReports;
    }

    /// <summary>Creates (or reuses) the feature block and opens this scenario's node in it.</summary>
    public void StartReportScenario(FeatureContext featureContext, ScenarioContext scenarioContext)
    {
        lock (_reportLock)
        {
            var reporter = GetOrCreateReporter();
            var feature = GetOrCreateFeature(reporter, featureContext.FeatureInfo.Title);

            _scenario = feature.CreateNode<Scenario>(scenarioContext.ScenarioInfo.Title);
        }
    }

    // One block per feature, shared by every scenario of the run; a per-scenario node duplicated
    // every feature in the report.
    internal static ExtentTest GetOrCreateFeature(ExtentReports reporter, string title)
    {
        if (!_featuresByTitle.TryGetValue(title, out var feature))
        {
            feature = reporter.CreateTest<Feature>(title);
            _featuresByTitle[title] = feature;
        }

        return feature;
    }

    /// <summary>
    /// Adds a labelled artifact path (trace, HAR) to the current scenario node, so the report points
    /// at every file a failure produced.
    /// </summary>
    /// <param name="label">Short artifact name, for example <c>Trace</c>.</param>
    /// <param name="path">Path of the artifact.</param>
    public void AttachArtifact(string label, string path)
    {
        lock (_reportLock)
        {
            if (_scenario is null)
            {
                return;
            }

            // The Spark template renders log entries on Gherkin step nodes, not on the scenario
            // node itself, so the artifacts live on one synthetic "Artifacts" step per scenario.
            _artifacts ??= _scenario.CreateNode(new GherkinKeyword("And"), "Artifacts");
            _ = _artifacts.Info($"{label}: {path}");
        }
    }

    /// <summary>
    /// Adds the finished step to the scenario's node; a failed <c>@ui</c> step also gets a
    /// screenshot.
    /// </summary>
    public async Task ReportStep(PlaywrightDriver playwrightDriver,
                                 FeatureContext featureContext,
                                 ScenarioContext scenarioContext)
    {
        StepInfo stepInfo = scenarioContext.StepContext.StepInfo;
        ExtentTest node = _scenario!.CreateNode(new GherkinKeyword(stepInfo.StepDefinitionType.ToString()), stepInfo.Text);

        if (scenarioContext.TestError is null)
        {
            return;
        }

        var IsUiScenario = scenarioContext.ScenarioInfo.CombinedTags.Contains("ui");
        if (!IsUiScenario)
        {
            _ = node.Fail(scenarioContext.TestError.Message);
            return;
        }

        var fileName = $"{featureContext.FeatureInfo.Title}_{scenarioContext.ScenarioInfo.Title}".Replace(" ", string.Empty);
        _ = node.Fail(scenarioContext.TestError.Message, new ScreenCapture
        {
            Title = "Error Screenshot",
            Path = await playwrightDriver.TakeScreenshotAsPathAsync(fileName)
        });
    }

    /// <summary>Flushes the report once, under the same lock that guards node creation.</summary>
    /// <param name="disposing">Whether managed resources are being disposed.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                // Flushing touches the shared report, which other scenarios may still be
                // appending to, so it happens under the same lock as node creation.
                lock (_reportLock)
                {
                    _extentReports?.Flush();
                }
            }

            _disposedValue = true;
        }
    }

    /// <summary>Flushes the report to <c>TestReports.html</c>.</summary>
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
