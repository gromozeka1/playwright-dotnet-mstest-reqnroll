using Framework.Config;
using Framework.Constants;
using Framework.Helpers;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reqnroll;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]
namespace Framework.Driver;
#pragma warning disable CA1822 // Mark members as static

[Binding]
public abstract class BaseHook
{
    [BeforeScenario(Order = int.MinValue)]
    public void VerifyMandatoryScenarioTags(FeatureContext featureContext, ScenarioContext scenarioContext, ExtentReportManager extentReportManager)
    {
        var scenarioTags = scenarioContext.ScenarioInfo.CombinedTags;
        var mandatoryTestTypeTags = new HashSet<string> { ScenarioTags.UI, ScenarioTags.API };
        var mandatoryEnvTags = new HashSet<string> { ScenarioTags.Local, ScenarioTags.Dev, ScenarioTags.QA, ScenarioTags.Stage, ScenarioTags.Prod };

        if (!mandatoryEnvTags.Any(x => scenarioTags.Contains(x)))
        {
            throw new ArgumentException($"Scenario should contain at least one env tag: {string.Join(", ", mandatoryEnvTags)}");
        }

        if (!mandatoryTestTypeTags.Any(x => scenarioTags.Contains(x)))
        {
            throw new ArgumentException($"Scenario should contain at least one test type tag: {string.Join(", ", mandatoryTestTypeTags)}");
        }
    }

    [BeforeScenario(Order = 10)]
    public void StartReportScenario(FeatureContext featureContext, ScenarioContext scenarioContext, ExtentReportManager extentReportManager)
    {
        extentReportManager.StartReportScenario(featureContext, scenarioContext);
    }

    [BeforeScenario(ScenarioTags.UI, Order = 20)]
    public async Task OpenApplication(PlaywrightDriver playwrightDriver)
    {
        await playwrightDriver.Page.GotoAsync(ConfigReader.Settings.ApplicationUrl);
    }

    [AfterStep]
    public async Task ReportStep(PlaywrightDriver playwrightDriver, FeatureContext featureContext, ScenarioContext scenarioContext, ExtentReportManager extentReportManager)
    {
        await extentReportManager.ReportStep(playwrightDriver, featureContext, scenarioContext);
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
