using Framework.Config;
using Framework.Driver;
using Microsoft.Playwright;

namespace Tests.Pages;

public class ProductListPage
{
    private readonly IPage _page;

    public ProductListPage(PlaywrightDriver playwrightDriver) => _page = playwrightDriver.Page.Result;

    public async Task NavigateAsync() => await _page.GotoAsync(ConfigReader.Settings.ApplicationUrl);

    private ILocator _lnkProductList => _page.GetByRole(AriaRole.Navigation)
        .GetByRole(AriaRole.List)
        .GetByRole(AriaRole.Link, new() { Name = "Products" });
    private ILocator _lnkCreate => _page.GetByRole(AriaRole.Link, new() { Name = "Add product" });
    public async Task CreateProductAsync()
    {
        await _lnkProductList.ClickAsync();
        await _lnkCreate.ClickAsync();
    }

    public async Task ClickProductFromList(string name)
    {
        await _page.GetByRole(AriaRole.Row, new() { Name = name })
            .GetByRole(AriaRole.Link, new() { Name = "Details" }).ClickAsync();
    }

    public ILocator IsProductCreated(string product)
    {
        return _page.GetByText(product, new() { Exact = true });
    }
}
