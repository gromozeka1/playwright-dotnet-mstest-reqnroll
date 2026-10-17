using Framework.Config;
using Framework.Driver;
using Microsoft.Playwright;

namespace Tests.Fixtures;

public class TestFixtureBase
{
    private readonly IPlaywrightDriver _playwrightDriver;
    private readonly TestSettings _testSettings;
    private Task<IPage> Page => _playwrightDriver.Page;

    public TestFixtureBase(IPlaywrightDriver playwrightDriver, TestSettings testSettings)
    {
        _playwrightDriver = playwrightDriver;
        _testSettings = testSettings;
    }

    public async Task NavigateToUrl() => await (await Page).GotoAsync(_testSettings.ApplicationUrl);

    public async Task TakeScreenshotAsync(string fileName) => await (await Page).ScreenshotAsync(new() { Path = fileName, FullPage = true });
}
