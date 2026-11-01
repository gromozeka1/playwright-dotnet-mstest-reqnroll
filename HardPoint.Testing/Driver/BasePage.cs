using Microsoft.Playwright;

namespace HardPoint.Testing.Driver;

/// <summary>
/// Base class for page objects: asynchronous ARIA-role locators over the scenario's page.
/// Consumers can always drop to raw Playwright through <see cref="GetPageAsync"/>.
/// </summary>
/// <param name="playwrightDriver">The scenario's driver, resolved by Reqnroll's DI container.</param>
public abstract class BasePage(PlaywrightDriver playwrightDriver)
{
    /// <summary>The scenario's page, starting the browser when it is not running yet.</summary>
    protected Task<IPage> GetPageAsync() => playwrightDriver.GetPageAsync();

    /// <summary>The link with the accessible name.</summary>
    protected async Task<ILocator> LinkAsync(string name)
        => (await GetPageAsync()).GetByRole(AriaRole.Link, new() { Name = name });

    /// <summary>The button with the accessible name.</summary>
    protected async Task<ILocator> ButtonAsync(string name)
        => (await GetPageAsync()).GetByRole(AriaRole.Button, new() { Name = name });

    /// <summary>The row with the accessible name.</summary>
    protected async Task<ILocator> RowAsync(string name)
        => (await GetPageAsync()).GetByRole(AriaRole.Row, new() { Name = name });

    /// <summary>The element with the given ARIA role and name inside the row with that name.</summary>
    protected async Task<ILocator> InRowAsync(string rowName, AriaRole role, string name)
        => (await RowAsync(rowName)).GetByRole(role, new() { Name = name });

    /// <summary>Fills the field with the given label.</summary>
    protected async Task FillAsync(string label, string value)
        => await (await GetPageAsync()).GetByLabel(label).FillAsync(value);

    /// <summary>Selects the option with the given value in the field with that label.</summary>
    protected async Task SelectAsync(string label, string value)
        => await (await GetPageAsync()).GetByLabel(label).SelectOptionAsync(value);

    /// <summary>Waits until the page reaches the given load state (default: <c>load</c>).</summary>
    protected async Task WaitForLoadStateAsync(LoadState state = LoadState.Load)
        => await (await GetPageAsync()).WaitForLoadStateAsync(state);

    /// <summary>Clicks the link with the accessible name.</summary>
    protected async Task ClickLinkAsync(string name)
        => await (await LinkAsync(name)).ClickAsync();

    /// <summary>Clicks the button with the accessible name.</summary>
    protected async Task ClickButtonAsync(string name)
        => await (await ButtonAsync(name)).ClickAsync();

    /// <summary>The element whose text matches exactly.</summary>
    public async Task<ILocator> TextAsync(string text)
        => (await GetPageAsync()).GetByText(text, new() { Exact = true });
}
