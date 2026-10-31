using Microsoft.Playwright;

namespace HardPoint.Testing.Driver;

public abstract class BasePage(PlaywrightDriver playwrightDriver)
{
    protected Task<IPage> GetPageAsync() => playwrightDriver.GetPageAsync();

    protected async Task<ILocator> LinkAsync(string name)
        => (await GetPageAsync()).GetByRole(AriaRole.Link, new() { Name = name });

    protected async Task<ILocator> ButtonAsync(string name)
        => (await GetPageAsync()).GetByRole(AriaRole.Button, new() { Name = name });

    protected async Task<ILocator> RowAsync(string name)
        => (await GetPageAsync()).GetByRole(AriaRole.Row, new() { Name = name });

    protected async Task ClickLinkAsync(string name)
        => await (await LinkAsync(name)).ClickAsync();

    protected async Task ClickButtonAsync(string name)
        => await (await ButtonAsync(name)).ClickAsync();

    public async Task<ILocator> TextAsync(string text)
        => (await GetPageAsync()).GetByText(text, new() { Exact = true });
}
