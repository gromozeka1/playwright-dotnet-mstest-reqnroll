using Framework.Driver;
using Microsoft.Playwright;
using Tests.Models;

namespace Tests.Pages;

public class ProductPage
{
    private readonly IPage _page;

    public ProductPage(PlaywrightDriver playwrightDriver)
    {
        _page = playwrightDriver.Page.Result;
    }

    private ILocator TxtName => _page.GetByLabel("Name");

    private ILocator TxtDescription => _page.GetByLabel("Description");

    private ILocator TxtPrice => _page.Locator("#Price");

    private ILocator SelectProduct => _page.GetByRole(AriaRole.Combobox, new() { Name = "Product type" });

    private ILocator LnkCreate => _page.GetByRole(AriaRole.Button, new() { Name = "Create product" });

    public async Task CreateProduct(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        await TxtName.FillAsync(product.Name);
        await TxtDescription.FillAsync(product.Description);
        await TxtPrice.FillAsync(product.Price.ToString());
        _ = await SelectProduct.SelectOptionAsync(((int)product.ProductType).ToString());
    }

    public async Task ClickCreate() => await LnkCreate.ClickAsync();
}