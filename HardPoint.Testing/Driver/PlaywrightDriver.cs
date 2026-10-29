using HardPoint.Testing.Config;
using HardPoint.Testing.Helpers;
using Microsoft.Playwright;

namespace HardPoint.Testing.Driver;

public class PlaywrightDriver : IDisposable
{
    // Characters that are invalid in a file name on Windows, plus spaces and the
    // separators that would break a path on either platform: traces are produced on
    // Linux CI and then downloaded, so both platforms must accept the name.
    private static readonly char[] _unsafeFileNameChars = ['<', '>', ':', '"', '/', '\\', '|', '?', '*', ' '];
    private readonly Lazy<Task<IPlaywright>> _playwright;
    private readonly Lazy<Task<IBrowser>> _browser;
    private readonly Lazy<Task<IBrowserContext>> _browserContext;
    private readonly Lazy<Task<IPage>> _page;
    private readonly Lazy<Task<IAPIRequestContext>> _apiRequestContext;
    private readonly TestSettings _testSettings = ConfigReader.Settings;
    private string? _tracePath;
    private bool _disposedValue;

    public PlaywrightDriver()
    {
        _playwright = new Lazy<Task<IPlaywright>>(Playwright.CreateAsync);
        _browser = new Lazy<Task<IBrowser>>(CreateBrowserAsync);
        _browserContext = new Lazy<Task<IBrowserContext>>(CreateBrowserContext);
        _page = new Lazy<Task<IPage>>(CreatePageAsync);
        _apiRequestContext = new Lazy<Task<IAPIRequestContext>>(CreateApiContextAsync);
    }

    public Task<IBrowser> Browser => _browser.Value;

    public Task<IBrowserContext> BrowserContext => _browserContext.Value;

    public IPage Page => _page.Value.Result;

    public Task<IAPIRequestContext> ApiRequestContext => _apiRequestContext.Value;

    // Traces are captured for every UI scenario but only kept (exported to *.zip)
    // when the scenario fails, so a failing CI run can be opened with
    // `playwright show-trace <file>.zip` without tracking traces for green runs.
    public async Task StartTracingAsync(string title)
    {
        _ = Directory.CreateDirectory(ArtifactDirectory.Traces);

        _tracePath = Path.Combine(ArtifactDirectory.Traces, $"{MakeSafeFileName(title)}-{Guid.NewGuid().ToString("N")[..8]}.zip");

        await (await BrowserContext).Tracing.StartAsync(new TracingStartOptions
        {
            Screenshots = true,
            Snapshots = true,
            Title = title
        });
    }

    public async Task<string?> StopTracingAsync(bool keepTrace)
    {
        if (_tracePath is null)
        {
            return null;
        }

        var tracePath = _tracePath;
        _tracePath = null;

        await (await BrowserContext).Tracing.StopAsync(new TracingStopOptions { Path = tracePath });

        if (!keepTrace)
        {
            File.Delete(tracePath);
            return null;
        }

        return tracePath;
    }

    private static string MakeSafeFileName(string name)
    {
        var safeName = new string([.. name.Select(c => _unsafeFileNameChars.Contains(c) ? '-' : c)]);

        return safeName.Length > 80 ? safeName[..80] : safeName;
    }

    public async Task<string> TakeScreenshotAsPathAsync(string fileName)
    {
        _ = Directory.CreateDirectory(ArtifactDirectory.Screenshots);

        var path = Path.Combine(ArtifactDirectory.Screenshots, $"{fileName}.png");
        _ = await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            FullPage = true,
            Type = ScreenshotType.Png,
            Path = path
        });

        return path;
    }

    private async Task<IBrowser> CreateBrowserAsync()
    {
        BrowserTypeLaunchOptions options = _testSettings.BrowserTypeLaunchOptions;
        IBrowserType browserType = _testSettings.BrowserType switch
        {
            BrowserType.Chromium => (await _playwright.Value).Chromium,
            BrowserType.Firefox => (await _playwright.Value).Firefox,
            BrowserType.Webkit => (await _playwright.Value).Webkit,
            _ => throw new ArgumentException($"Unsupported browser type: {_testSettings.BrowserType}"),
        };

        return await browserType.LaunchAsync(options);
    }

    private async Task<IAPIRequestContext> CreateApiContextAsync()
    {
        APIRequestNewContextOptions options = _testSettings.APIRequestNewContextOptions;
        return await (await _playwright.Value).APIRequest.NewContextAsync(options);
    }

    private async Task<IPage> CreatePageAsync() => await (await _browserContext.Value).NewPageAsync();

    private async Task<IBrowserContext> CreateBrowserContext()
    {
        var harPath = Path.Combine(ArtifactDirectory.Root, $"sut-{Guid.NewGuid():N}.har");
        return await (await _browser.Value).NewContextAsync(new BrowserNewContextOptions
        {
            RecordHarPath = harPath,
            RecordHarUrlFilterString = "**/Product/**"
        });
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposedValue)
        {
            return;
        }

        if (disposing)
        {
            // dispose managed state (managed objects)
            if (_browser.IsValueCreated)
            {
                Task.Run(async () =>
                {
                    await Page.CloseAsync();
                    await Task.Delay(TimeSpan.FromMilliseconds(500));
                    await (await BrowserContext).CloseAsync();
                    await Task.Delay(TimeSpan.FromMilliseconds(500));
                    await (await Browser).CloseAsync();
                    await (await Browser).DisposeAsync();
                }).Wait();
            }

            if (_apiRequestContext.IsValueCreated)
            {
                _ = Task.Run(async () => await (await ApiRequestContext).DisposeAsync());
            }
        }

        // free unmanaged resources (unmanaged objects) and override finalizer
        // set large fields to null
        _disposedValue = true;
    }

    // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
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
