using System.Reflection;
using Framework.Config;
using Framework.Constants;
using Framework.Helpers;

namespace Framework.Driver;

public class PlaywrightDriver : IPlaywrightDriver, IDisposable
{
    private readonly AsyncLazy<IBrowser> _browser;
    private readonly AsyncLazy<IBrowserContext> _browserContext;
    private readonly AsyncLazy<IPage> _page;
    private readonly AsyncLazy<IAPIRequestContext> _apiRequestContext;
    private readonly TestSettings _testSettings = ConfigReader.Settings;
    private bool _disposedValue;

    public PlaywrightDriver()
    {
        _browser = new AsyncLazy<IBrowser>(CreateBrowserAsync);
        _browserContext = new AsyncLazy<IBrowserContext>(CreateBrowserContext);
        _page = new AsyncLazy<IPage>(CreatePageAsync);
        _apiRequestContext = new AsyncLazy<IAPIRequestContext>(CreateApiContext);
    }

    public Task<IPage> Page => _page.Value;

    public Task<IBrowser> Browser => _browser.Value;

    public Task<IBrowserContext> BrowserContext => _browserContext.Value;

    public Task<IAPIRequestContext> ApiRequestContext => _apiRequestContext.Value;

    public async Task<string> TakeScreenshotAsPathAsync(string fileName)
    {
        var path = $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}//{fileName}.png";
        _ = await Page.Result.ScreenshotAsync(new PageScreenshotOptions
        {
            FullPage = true,
            Type = ScreenshotType.Png,
            Path = path
        });

        return path;
    }

    private async Task<IBrowser> CreateBrowserAsync()
    {
        IPlaywright playwright = await Playwright.CreateAsync();
        BrowserTypeLaunchOptions options = GetOptions();
        var driverType = _testSettings.DriverType.ToString().ToLower();
        return await playwright[driverType].LaunchAsync(options);
    }

    private BrowserTypeLaunchOptions GetOptions()
    {
        var timeout = _testSettings.Timeout ?? SettingConstants.DEFAULT_TIMEOUT_SECONDS;
        var options = new BrowserTypeLaunchOptions
        {
            Args = _testSettings.Args,
            Timeout = TimeConverter.ToMilliseconds(timeout),
            Headless = _testSettings.Headless,
            SlowMo = _testSettings.SlowMo
        };

        options.Channel = _testSettings.DriverType switch
        {
            DriverType.Chrome => "chrome",
            DriverType.Firefox => "firefox",
            DriverType.Edge => "msedge",
            DriverType.Chromium => "chromium",
            _ => "chromium"
        };

        return options;
    }

    private async Task<IAPIRequestContext> CreateApiContext()
    {
        IPlaywright playwright = await Playwright.CreateAsync();

        return await playwright.APIRequest.NewContextAsync(new APIRequestNewContextOptions()
        {
            BaseURL = _testSettings.ApplicationApiUrl,
            // can be moved to config
            IgnoreHTTPSErrors = true
        });
    }

    private async Task<IPage> CreatePageAsync()
    {
        return await (await _browser).NewPageAsync(new BrowserNewPageOptions()
        {
            //ToDo: move to config
            RecordHarPath = $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/eapp.har",
            RecordHarUrlFilterString = "**/Product/**"
        });
    }

    private async Task<IBrowserContext> CreateBrowserContext() => await (await _browser).NewContextAsync();

    protected virtual void Dispose(bool disposing)
    {
        if (_disposedValue)
        {
            return;
        }

        if (disposing)
        {
            // TODO: dispose managed state (managed objects)
            if (_browser.IsValueCreated)
            {
                Task.Run(async () =>
                {
                    await (await Page).CloseAsync();
                    await Task.Delay(TimeSpan.FromMilliseconds(500));
                    await (await BrowserContext).CloseAsync();
                    await Task.Delay(TimeSpan.FromMilliseconds(500));
                    await (await Browser).CloseAsync();
                    await (await Browser).DisposeAsync();
                }).Wait();
            }

            if (_apiRequestContext.IsValueCreated)
            {
                _ = Task.Run(async () =>
                {
                    await (await ApiRequestContext).DisposeAsync();
                });
            }
        }

        // TODO: free unmanaged resources (unmanaged objects) and override finalizer
        // TODO: set large fields to null
        _disposedValue = true;
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~PlaywrightDriver()
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
