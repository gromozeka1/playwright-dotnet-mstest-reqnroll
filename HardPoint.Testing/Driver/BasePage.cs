using Microsoft.Playwright;

namespace HardPoint.Testing.Driver;

public abstract class BasePage(PlaywrightDriver playwrightDriver)
{
    protected IPage Page { get; } = playwrightDriver.Page;

    protected ILocator Link(string name) => Page.GetByRole(AriaRole.Link, new() { Name = name });

    protected ILocator Button(string name) => Page.GetByRole(AriaRole.Button, new() { Name = name });

    protected ILocator Row(string name) => Page.GetByRole(AriaRole.Row, new() { Name = name });

    protected async Task ClickLinkAsync(string name) => await Link(name).ClickAsync();

    protected async Task ClickButtonAsync(string name) => await Button(name).ClickAsync();

    public ILocator Text(string text) => Page.GetByText(text, new() { Exact = true });
}
