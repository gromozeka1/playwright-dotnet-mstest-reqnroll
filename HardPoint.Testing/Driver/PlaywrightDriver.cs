using HardPoint.Testing.Config;
using HardPoint.Testing.Helpers;
using Microsoft.Playwright;

namespace HardPoint.Testing.Driver;

/// <summary>
/// One browser, one browser context and one API request context per scenario, created lazily and
/// disposed in order. Reqnroll resolves it from its DI container.
/// </summary>
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
    private string? _harPath;
    private bool _disposedValue;

    /// <summary>Creates the driver from the current <see cref="ConfigReader.Settings"/>.</summary>
    public PlaywrightDriver()
    {
        _playwright = new Lazy<Task<IPlaywright>>(Playwright.CreateAsync);
        _browser = new Lazy<Task<IBrowser>>(CreateBrowserAsync);
        _browserContext = new Lazy<Task<IBrowserContext>>(CreateBrowserContext);
        _page = new Lazy<Task<IPage>>(CreatePageAsync);
        _apiRequestContext = new Lazy<Task<IAPIRequestContext>>(CreateApiContextAsync);
    }

    /// <summary>The browser, launched on first access.</summary>
    public Task<IBrowser> Browser => _browser.Value;

    /// <summary>The browser context, created on first access from the configured context options.</summary>
    public Task<IBrowserContext> BrowserContext => _browserContext.Value;

    /// <summary>Starts the browser (when it is not running yet) and returns the scenario's page.</summary>
    public Task<IPage> GetPageAsync() => _page.Value;

    /// <summary>The API request context, created on first access.</summary>
    public Task<IAPIRequestContext> ApiRequestContext => _apiRequestContext.Value;

    /// <summary>
    /// The HAR archive written for this scenario's browser context, or <see langword="null"/> when
    /// recording is off. Useful for attaching the artifact to a report of your own.
    /// </summary>
    public string? HarPath => _harPath;

    /// <summary>
    /// Starts a Playwright trace for the scenario. The archive is kept only when
    /// <see cref="StopTracingAsync"/> is asked to keep it, so green runs do not accumulate traces.
    /// </summary>
    /// <param name="title">Scenario title, used to name the trace archive.</param>
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

    /// <summary>
    /// Stops the trace started by <see cref="StartTracingAsync"/> and keeps it only when
    /// <paramref name="keepTrace"/> is <see langword="true"/>.
    /// </summary>
    /// <param name="keepTrace">Keep the archive (a failing scenario) or delete it.</param>
    /// <returns>The archive path when it was kept; otherwise <see langword="null"/>.</returns>
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

    /// <summary>Takes a full-page screenshot into the artifact directory.</summary>
    /// <param name="fileName">File name without the extension.</param>
    /// <returns>The path of the written PNG.</returns>
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
    /// Builds the context options for one scenario: the consumer's
    /// <see cref="TestSettings.BrowserContextOptions"/> are passed through, and the framework then
    /// owns the HAR settings, routing the archive to the artifact directory unless
    /// <see cref="HarOptions.RecordHar"/> turns recording off.
    /// </summary>
    internal BrowserNewContextOptions CreateBrowserContextOptions()
    {
        // Copied per context: every scenario shares one settings instance, and the HAR path below
        // is per context, so writing it into the shared object would race between parallel runs.
        var options = new BrowserNewContextOptions(_testSettings.BrowserContextOptions);

        HarOptions harOptions = _testSettings.HarOptions;
        if (!harOptions.RecordHar)
        {
            // Not the package's archive to record; a consumer who configured their own HAR in
            // browserContextOptions keeps it.
            return options;
        }

        // The framework wins on the archive so every HAR lands next to the report, whatever the
        // consumer configured, and the filter stays in one place (harOptions).
        _harPath = Path.Combine(ArtifactDirectory.Root, $"sut-{Guid.NewGuid():N}.har");
        options.RecordHarPath = _harPath;
        options.RecordHarUrlFilterString = string.IsNullOrWhiteSpace(harOptions.UrlFilter) ? null : harOptions.UrlFilter;
        options.RecordHarUrlFilter = null;
        options.RecordHarUrlFilterRegex = null;

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
