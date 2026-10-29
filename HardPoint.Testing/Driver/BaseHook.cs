using HardPoint.Testing.Config;
using HardPoint.Testing.Constants;
using HardPoint.Testing.Helpers;
using Microsoft.Playwright;
using Reqnroll;

namespace HardPoint.Testing.Driver;
#pragma warning disable CA1822 // Mark members as static

[Binding]
public abstract class BaseHook
{
    [BeforeScenario(Order = int.MinValue)]
    public void VerifyMandatoryScenarioTags(ScenarioContext scenarioContext)
    {
        if (!ConfigReader.Settings.EnforceScenarioTags)
        {
            return;
        }

        var scenarioTags = scenarioContext.ScenarioInfo.CombinedTags;

        if (!ScenarioTags.MandatoryEnvTags.Any(x => scenarioTags.Contains(x)))
        {
            throw new ArgumentException($"Scenario should contain at least one env tag: {string.Join(", ", ScenarioTags.MandatoryEnvTags)}");
        }

        if (!ScenarioTags.MandatoryTestTypeTags.Any(x => scenarioTags.Contains(x)))
        {
            throw new ArgumentException($"Scenario should contain at least one test type tag: {string.Join(", ", ScenarioTags.MandatoryTestTypeTags)}");
        }
    }

    [BeforeScenario(Order = 10)]
    public void StartReportScenario(FeatureContext featureContext, ScenarioContext scenarioContext, ExtentReportManager extentReportManager)
    {
        extentReportManager.StartReportScenario(featureContext, scenarioContext);
    }

    [BeforeScenario(ScenarioTags.UI, Order = 20)]
    public async Task OpenApplication(PlaywrightDriver playwrightDriver, ScenarioContext scenarioContext)
    {
        await playwrightDriver.StartTracingAsync(scenarioContext.ScenarioInfo.Title);
        await playwrightDriver.Page.GotoAsync(ConfigReader.Settings.ApplicationUrl);
    }

    [AfterStep]
    public async Task ReportStep(PlaywrightDriver playwrightDriver, FeatureContext featureContext, ScenarioContext scenarioContext, ExtentReportManager extentReportManager)
    {
        await extentReportManager.ReportStep(playwrightDriver, featureContext, scenarioContext);
    }

    [AfterScenario(ScenarioTags.UI)]
    public async Task SaveTraceOnFailure(ScenarioContext scenarioContext, PlaywrightDriver playwrightDriver, IReqnrollOutputHelper output)
    {
        string? tracePath = await playwrightDriver.StopTracingAsync(scenarioContext.TestError is not null);

        if (tracePath is not null)
        {
            output.AddAttachment(tracePath);
        }
    }

    [AfterScenario(ScenarioTags.UI)]
    public async Task AttachScreenshotOnFailure(ScenarioContext scenarioContext, PlaywrightDriver playwrightDriver, IReqnrollOutputHelper output)
    {
        //if (scenarioContext.TestError is null)
        //{
        //    return;
        //}

        var directory = Path.Combine(AppContext.BaseDirectory, "screenshots");

        _ = Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, $"{Guid.NewGuid():N}.png");

        _ = await playwrightDriver.Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = path,
            FullPage = true
        });

        output.AddAttachment(path);
    }

    [AfterTestRun]
    public static void AfterTestRun(PlaywrightDriver playwrightDriver, ExtentReportManager extentReportManager)
    {
        extentReportManager.Dispose();
        playwrightDriver.Dispose();
    }
}
