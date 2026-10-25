using AventStack.ExtentReports;
using AventStack.ExtentReports.Gherkin;
using AventStack.ExtentReports.Gherkin.Model;
using AventStack.ExtentReports.Model;
using AventStack.ExtentReports.Reporter;
using Framework.Config;
using Framework.Driver;
using Reqnroll;

namespace Framework.Helpers;

public class ExtentReportManager : IDisposable
{
    private static ExtentReports? _extentReports;
    private ExtentTest? _scenario;
    private bool _disposedValue;

    public ExtentReportManager()
    {
        if (_extentReports is not null)
        {
            return;
        }

        lock (new Lock())
        {
            if (_extentReports is null)
            {
                _extentReports = new ExtentReports();
                _extentReports.AddSystemInfo("os", Environment.OSVersion.ToString());
                _extentReports.AddSystemInfo("browser", ConfigReader.Settings.BrowserType);
                _extentReports.AttachReporter(new ExtentSparkReporter("TestReports.html"));
            }
        }
    }

    public void StartReportScenario(FeatureContext featureContext, ScenarioContext scenarioContext)
    {
        ExtentTest feature = _extentReports!.CreateTest<Feature>(featureContext.FeatureInfo.Title);
        _scenario = feature.CreateNode<Scenario>(scenarioContext.ScenarioInfo.Title);
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
                // TODO: dispose managed state (managed objects)
                _extentReports?.Flush();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            _disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~ExtentReportManager()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
