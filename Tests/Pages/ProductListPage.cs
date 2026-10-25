using Framework.Driver;
using Microsoft.Playwright;

namespace Tests.Pages;

public class ProductListPage(PlaywrightDriver playwrightDriver) : BasePage(playwrightDriver)
{
    private ILocator LnkProductList => Page.GetByRole(AriaRole.Navigation)
        .GetByRole(AriaRole.List)
        .GetByRole(AriaRole.Link, new() { Name = "Products" });

    public async Task CreateProductAsync()
    {
        await LnkProductList.ClickAsync();
        await ClickLinkAsync("Add product");
    }

    public async Task ClickProductFromList(string name)
        => await Row(name).GetByRole(AriaRole.Link, new() { Name = "Details" }).ClickAsync();

    public ILocator IsProductCreated(string product) => Text(product);
}
