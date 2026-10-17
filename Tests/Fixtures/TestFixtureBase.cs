using Framework.Config;
using Framework.Driver;
using Microsoft.Playwright;

namespace Tests.Fixtures;

public class TestFixtureBase
{
    private readonly IPlaywrightDriver _playwrightDriver;
    private Task<IPage> Page => _playwrightDriver.Page;

    public TestFixtureBase(IPlaywrightDriver playwrightDriver)
    {
        _playwrightDriver = playwrightDriver;
    }

    public async Task NavigateToUrl() => await (await Page).GotoAsync(ConfigReader.Settings.ApplicationUrl);

    public async Task TakeScreenshotAsync(string fileName) => await (await Page).ScreenshotAsync(new() { Path = fileName, FullPage = true });
}
