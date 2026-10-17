using System.Reflection;
using System.Runtime.ExceptionServices;
using Framework.Config;
using Framework.Constants;
using Framework.Helpers;

namespace Framework.Driver;

public class PlaywrightDriver : IDisposable
{
    private readonly Lazy<Task<IPlaywright>> _playwright;
    private readonly Lazy<Task<IBrowser>> _browser;
    private readonly Lazy<Task<IBrowserContext>> _browserContext;
    private readonly Lazy<Task<IPage>> _page;
    private readonly Lazy<Task<IAPIRequestContext>> _apiRequestContext;
    private readonly TestSettings _testSettings = ConfigReader.Settings;
    private int _disposed;

    public PlaywrightDriver()
    {
        _playwright = new Lazy<Task<IPlaywright>>(Playwright.CreateAsync);
        _browser = new Lazy<Task<IBrowser>>(CreateBrowserAsync);
        _browserContext = new Lazy<Task<IBrowserContext>>(CreateBrowserContext);
        _page = new Lazy<Task<IPage>>(CreatePageAsync);
        _apiRequestContext = new Lazy<Task<IAPIRequestContext>>(CreateApiContext);
    }

    public Task<IPage> Page
    {
        get
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
            return _page.Value;
        }
    }

    public Task<IBrowser> Browser
    {
        get
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
            return _browser.Value;
        }
    }

    public Task<IBrowserContext> BrowserContext
    {
        get
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
            return _browserContext.Value;
        }
    }

    public Task<IAPIRequestContext> ApiRequestContext
    {
        get
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
            return _apiRequestContext.Value;
        }
    }

    public async Task<string> TakeScreenshotAsPathAsync(string fileName)
    {
        var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, $"{fileName}.png");
        _ = await (await Page).ScreenshotAsync(new PageScreenshotOptions
        {
            FullPage = true,
            Type = ScreenshotType.Png,
            Path = path
        });

        return path;
    }

    private async Task<IBrowser> CreateBrowserAsync()
    {
        BrowserTypeLaunchOptions options = GetOptions();
        var driverType = _testSettings.DriverType.ToString().ToLowerInvariant();
        return await (await _playwright.Value)[driverType].LaunchAsync(options);
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
        return await (await _playwright.Value).APIRequest.NewContextAsync(new APIRequestNewContextOptions
        {
            BaseURL = _testSettings.ApplicationApiUrl,
            // can be moved to config
            IgnoreHTTPSErrors = true
        });
    }

    private async Task<IPage> CreatePageAsync() => await (await _browserContext.Value).NewPageAsync();

    private async Task<IBrowserContext> CreateBrowserContext()
    {
        var harPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, $"eapp-{Guid.NewGuid():N}.har");
        return await (await _browser.Value).NewContextAsync(new BrowserNewContextOptions
        {
            RecordHarPath = harPath,
            RecordHarUrlFilterString = "**/Product/**"
        });
    }

    private async ValueTask DisposeResourcesAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        List<Exception>? exceptions = null;

        async ValueTask DisposeResourceAsync(Func<Task> dispose)
        {
            try
            {
                await dispose();
            }
            catch (Exception exception)
            {
                (exceptions ??= []).Add(exception);
            }
        }

        if (_apiRequestContext.IsValueCreated)
        {
            await DisposeResourceAsync(async () => await (await _apiRequestContext.Value).DisposeAsync());
        }

        if (_page.IsValueCreated)
        {
            await DisposeResourceAsync(async () => await (await _page.Value).CloseAsync());
        }

        if (_browserContext.IsValueCreated)
        {
            await DisposeResourceAsync(async () => await (await _browserContext.Value).CloseAsync());
        }

        if (_browser.IsValueCreated)
        {
            IBrowser? browser = null;
            await DisposeResourceAsync(async () =>
            {
                browser = await _browser.Value;
                await browser.CloseAsync();
            });
            if (browser is not null)
            {
                await DisposeResourceAsync(async () => await browser.DisposeAsync());
            }
        }

        if (_playwright.IsValueCreated)
        {
            await DisposeResourceAsync(async () => (await _playwright.Value).Dispose());
        }

        if (exceptions is { Count: 1 })
        {
            ExceptionDispatchInfo.Capture(exceptions[0]).Throw();
        }

        if (exceptions is { Count: > 1 })
        {
            throw new AggregateException("One or more Playwright resources could not be disposed.", exceptions);
        }

        GC.SuppressFinalize(this);
    }

    public void Dispose() => DisposeResourcesAsync().AsTask().GetAwaiter().GetResult();
}
