namespace Framework.Driver;

public interface IPlaywrightDriver
{
    Task<IPage> Page { get; }
    Task<IBrowser> Browser { get; }
    Task<IBrowserContext> BrowserContext { get; }
    Task<IAPIRequestContext> ApiRequestContext { get; }
    Task<string> TakeScreenshotAsPathAsync(string fileName);
}
