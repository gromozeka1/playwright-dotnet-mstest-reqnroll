using System.Reflection;
using HardPoint.Testing.Config;
using HardPoint.Testing.Constants;
using HardPoint.Testing.Helpers;
using Microsoft.Playwright;
using Reqnroll;

namespace HardPoint.Testing.Driver;
#pragma warning disable CA1822 // Mark members as static

/// <summary>
/// The Reqnroll <c>[Binding]</c> a consumer derives from: validates the scenario tags, opens the
/// report node, navigates <c>@ui</c> scenarios to the application, keeps failure artifacts, and
/// disposes the report and driver after the run. Reqnroll discovers it from the package reference.
/// </summary>
[Binding]
public abstract class BaseHook
{
    // Full name of MSTest's [assembly: Parallelize]. Matched by name, not by type: the package has
    // no compile-time MSTest reference (MSTest arrives through Reqnroll.MSTest at whatever version
    // the consumer pins), and taking one would bind the check to an assembly identity that may not
    // be the one the test host loaded.
    private const string _parallelizeAttributeName = "Microsoft.VisualStudio.TestTools.UnitTesting.ParallelizeAttribute";

    // Every scenario runs this hook, so the warning is written once per test process: repeating
    // it in every scenario's output would bury the message instead of surfacing it.
    private static int _parallelismWarningWritten;

    /// <summary>
    /// Rejects a scenario that has no environment tag or no test-type tag, when
    /// <c>enforceScenarioTags</c> is on.
    /// </summary>
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

    /// <summary>
    /// Warns once when the consuming test assembly has no <c>[assembly: Parallelize]</c>: without
    /// it MSTest runs every scenario serially, and the suite still passes, only slower.
    /// </summary>
    /// <remarks>
    /// A warning rather than an exception keeps deliberately serial suites working; they can also
    /// turn it off with the <see cref="TestSettings.WarnOnSerialExecution"/> setting.
    /// </remarks>
    [BeforeScenario(Order = int.MinValue + 1)]
    public void WarnWhenParallelismIsNotConfigured(IReqnrollOutputHelper output)
    {
        if (!ConfigReader.Settings.WarnOnSerialExecution
            || Interlocked.Exchange(ref _parallelismWarningWritten, 1) != 0)
        {
            return;
        }

        var assembly = GetType().Assembly;
        if (DeclaresParallelize(assembly))
        {
            return;
        }

        output.WriteLine(
            $"HardPoint.Testing: '{assembly.GetName().Name}' declares no [assembly: Parallelize] attribute, " +
            "so its scenarios run serially. Add [assembly: Parallelize(Scope = ExecutionScope.MethodLevel)] " +
            "to the test assembly, or set warnOnSerialExecution to false to silence this warning. " +
            "See the package README.");
    }

    internal static bool DeclaresParallelize(Assembly assembly)
        => assembly.GetCustomAttributesData().Any(attribute =>
            string.Equals(attribute.AttributeType.FullName, _parallelizeAttributeName, StringComparison.Ordinal));

    // Unit tests reset the one-time guard so each case can observe the warning.
    internal static void ResetParallelismWarning() => _parallelismWarningWritten = 0;

    /// <summary>Opens the scenario's node in the Extent report.</summary>
    [BeforeScenario(Order = 10)]
    public void StartReportScenario(FeatureContext featureContext, ScenarioContext scenarioContext, ExtentReportManager extentReportManager)
    {
        extentReportManager.StartReportScenario(featureContext, scenarioContext);
    }

    /// <summary>
    /// Starts the scenario's trace and navigates to <c>applicationUrl</c>; runs for <c>@ui</c>
    /// scenarios only.
    /// </summary>
    [BeforeScenario(ScenarioTags.UI, Order = 20)]
    public async Task OpenApplication(PlaywrightDriver playwrightDriver, ScenarioContext scenarioContext)
    {
        await playwrightDriver.StartTracingAsync(scenarioContext.ScenarioInfo.Title);
        IPage page = await playwrightDriver.GetPageAsync();
        await page.GotoAsync(ConfigReader.Settings.ApplicationUrl);
    }

    /// <summary>Adds the finished step to the report, with a screenshot when it failed.</summary>
    [AfterStep]
    public async Task ReportStep(PlaywrightDriver playwrightDriver, FeatureContext featureContext, ScenarioContext scenarioContext, ExtentReportManager extentReportManager)
    {
        await extentReportManager.ReportStep(playwrightDriver, featureContext, scenarioContext);
    }

    /// <summary>Keeps the trace archive of a failed <c>@ui</c> scenario and attaches its path.</summary>
    [AfterScenario(ScenarioTags.UI)]
    public async Task SaveTraceOnFailure(ScenarioContext scenarioContext, PlaywrightDriver playwrightDriver, IReqnrollOutputHelper output)
    {
        string? tracePath = await playwrightDriver.StopTracingAsync(scenarioContext.TestError is not null);

        if (tracePath is not null)
        {
            output.AddAttachment(tracePath);
        }
    }

    /// <summary>Attaches a screenshot when a <c>@ui</c> scenario failed.</summary>
    [AfterScenario(ScenarioTags.UI)]
    public async Task AttachScreenshotOnFailure(ScenarioContext scenarioContext, PlaywrightDriver playwrightDriver, IReqnrollOutputHelper output)
    {
        if (scenarioContext.TestError is null)
        {
            return;
        }

        _ = Directory.CreateDirectory(ArtifactDirectory.Screenshots);

        var path = Path.Combine(ArtifactDirectory.Screenshots, $"{Guid.NewGuid():N}.png");

        IPage page = await playwrightDriver.GetPageAsync();
        _ = await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = path,
            FullPage = true
        });

        output.AddAttachment(path);
    }

    /// <summary>Flushes the report and disposes the driver after the whole run.</summary>
    [AfterTestRun]
    public static async Task AfterTestRun(PlaywrightDriver playwrightDriver, ExtentReportManager extentReportManager)
    {
        extentReportManager.Dispose();
        await playwrightDriver.DisposeAsync();
    }
}
