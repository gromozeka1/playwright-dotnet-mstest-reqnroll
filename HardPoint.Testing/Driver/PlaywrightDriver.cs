using HardPoint.Testing.Config;
using HardPoint.Testing.Helpers;
using Microsoft.Playwright;

namespace HardPoint.Testing.Driver;

public class PlaywrightDriver : IDisposable, IAsyncDisposable
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

    /// <summary>Starts the browser (when it is not running yet) and returns the scenario's page.</summary>
    public Task<IPage> GetPageAsync() => _page.Value;

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
        IPage page = await GetPageAsync();
        _ = await page.ScreenshotAsync(new PageScreenshotOptions
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
        => await (await _browser.Value).NewContextAsync(CreateBrowserContextOptions());

    /// <summary>
    /// Builds the context options the package owns: a HAR archive routed to the artifact
    /// directory unless <see cref="HarOptions.RecordHar"/> turns recording off, filtered by
    /// <see cref="HarOptions.UrlFilter"/> rather than by any URL pattern from one test suite.
    /// </summary>
    internal BrowserNewContextOptions CreateBrowserContextOptions()
    {
        HarOptions harOptions = _testSettings.HarOptions;
        var options = new BrowserNewContextOptions();

        if (!harOptions.RecordHar)
        {
            return options;
        }

        options.RecordHarPath = Path.Combine(ArtifactDirectory.Root, $"sut-{Guid.NewGuid():N}.har");

        // An empty filter means "record every request"; Playwright treats an absent filter the
        // same way, so there is no need to hand it a pattern that would only add noise.
        if (!string.IsNullOrWhiteSpace(harOptions.UrlFilter))
        {
            options.RecordHarUrlFilterString = harOptions.UrlFilter;
        }

        return options;
    }

    /// <summary>
    /// Closes the page, context, browser, API context and the Playwright driver, in that order.
    /// Awaiting each step replaces the old <c>Task.Run(...).Wait()</c> with two fixed 500 ms delays
    /// that guessed when the previous step had finished.
    /// </summary>
    /// <remarks>
    /// Teardown runs after the scenario has already reported its result, and a browser that is slow
    /// or already gone must not turn a passing scenario into a failure — so every step is
    /// best-effort and the first failure does not skip the rest of the cleanup.
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        if (_disposedValue)
        {
            return;
        }

        _disposedValue = true;

        if (_page.IsValueCreated)
        {
            await IgnoreAsync(async () => await (await _page.Value).CloseAsync());
        }

        if (_browserContext.IsValueCreated)
        {
            await IgnoreAsync(async () => await (await _browserContext.Value).CloseAsync());
        }

        if (_browser.IsValueCreated)
        {
            await IgnoreAsync(async () =>
            {
                IBrowser browser = await _browser.Value;
                await browser.CloseAsync();
                await browser.DisposeAsync();
            });
        }

        if (_apiRequestContext.IsValueCreated)
        {
            await IgnoreAsync(async () => await (await _apiRequestContext.Value).DisposeAsync());
        }

        if (_playwright.IsValueCreated)
        {
            await IgnoreAsync(async () => (await _playwright.Value).Dispose());
        }

        GC.SuppressFinalize(this);
    }

    private static async Task IgnoreAsync(Func<Task> teardown)
    {
        try
        {
            await teardown();
        }
        catch (Exception)
        {
            // Best effort, see the remarks on DisposeAsync.
        }
    }

    /// <summary>
    /// Synchronous teardown for callers that cannot await (the Reqnroll hook is async, framework
    /// consumers usually are not). It blocks this thread on <see cref="DisposeAsync"/> rather than
    /// scheduling the work on another one.
    /// </summary>
    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
