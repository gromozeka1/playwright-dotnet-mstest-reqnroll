using AventStack.ExtentReports;
using AventStack.ExtentReports.Gherkin;
using AventStack.ExtentReports.Gherkin.Model;
using AventStack.ExtentReports.Model;
using AventStack.ExtentReports.Reporter;
using HardPoint.Testing.Config;
using HardPoint.Testing.Driver;
using Reqnroll;

namespace HardPoint.Testing.Helpers;

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
    private bool _disposedValue;

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

    public void StartReportScenario(FeatureContext featureContext, ScenarioContext scenarioContext)
    {
        lock (_reportLock)
        {
            var reporter = GetOrCreateReporter();

            if (!_featuresByTitle.TryGetValue(featureContext.FeatureInfo.Title, out var feature))
            {
                feature = reporter.CreateTest<Feature>(featureContext.FeatureInfo.Title);
                _featuresByTitle[featureContext.FeatureInfo.Title] = feature;
            }

            _scenario = feature.CreateNode<Scenario>(scenarioContext.ScenarioInfo.Title);
        }
    }

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

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
