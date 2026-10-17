using AventStack.ExtentReports;
using AventStack.ExtentReports.Gherkin;
using AventStack.ExtentReports.Gherkin.Model;
using AventStack.ExtentReports.Model;
using AventStack.ExtentReports.Reporter;
using Framework.Config;
using Framework.Driver;
using Reqnroll;
using Reqnroll.BoDi;
using Tests.Models;
using Tests.Pages;
using Tests.Support;

namespace Tests.Hooks;

[Binding]
public class Hooks(
    PlaywrightDriver playwrightDriver,
    IObjectContainer container,
    ProductsApi productsApi,
    FeatureContext featureContext,
    ScenarioContext scenarioContext)
{
    private static ExtentReports? _extentReports;
    private ExtentTest? _scenario;

    private bool IsUiScenario => scenarioContext.ScenarioInfo.CombinedTags.Contains("ui");

    [BeforeTestRun]
    public static void InitializeExtentReports()
    {
        _extentReports = new ExtentReports();
        _extentReports.AddSystemInfo("os", Environment.OSVersion.ToString());
        _extentReports.AddSystemInfo("browser", ConfigReader.Settings.DriverType.ToString());
        _extentReports.AttachReporter(new ExtentSparkReporter("TestReports.html"));
    }

    [BeforeScenario(Order = 10)]
    public void StartReportScenario()
    {
        ExtentTest feature = _extentReports!.CreateTest<Feature>(featureContext.FeatureInfo.Title);
        _scenario = feature.CreateNode<Scenario>(scenarioContext.ScenarioInfo.Title);
    }

    // Resolved here, not injected: page objects create the browser page, which API scenarios must not do.
    [BeforeScenario("ui", Order = 20)]
    public async Task OpenApplication() => await container.Resolve<ProductListPage>().NavigateAsync();

    [AfterStep]
    public async Task ReportStep()
    {
        var stepInfo = scenarioContext.StepContext.StepInfo;
        ExtentTest node = _scenario!.CreateNode(new GherkinKeyword(stepInfo.StepDefinitionType.ToString()), stepInfo.Text);

        if (scenarioContext.TestError is null)
        {
            return;
        }

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

    [AfterScenario]
    public async Task DeleteCreatedProduct()
    {
        if (scenarioContext.TryGetValue(out Product product))
        {
            await productsApi.DeleteByNameAsync(product.Name);
        }
    }

    [AfterTestRun]
    public static void TearDownReport() => _extentReports?.Flush();
}
