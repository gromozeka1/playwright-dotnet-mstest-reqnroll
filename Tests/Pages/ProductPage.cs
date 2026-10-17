using Framework.Driver;
using Microsoft.Playwright;
using Tests.Models;

namespace Tests.Pages;

public class ProductPage(PlaywrightDriver playwrightDriver) : BasePage(playwrightDriver)
{
    private ILocator TxtName => Page.GetByLabel("Name");

    private ILocator TxtDescription => Page.GetByLabel("Description");

    private ILocator TxtPrice => Page.Locator("#Price");

    private ILocator SelectProduct => Page.GetByRole(AriaRole.Combobox, new() { Name = "Product type" });

    public async Task CreateProduct(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        await TxtName.FillAsync(product.Name);
        await TxtDescription.FillAsync(product.Description);
        await TxtPrice.FillAsync(product.Price.ToString());
        _ = await SelectProduct.SelectOptionAsync(((int)product.ProductType).ToString());
    }

    public async Task ClickCreate() => await ClickButtonAsync("Create product");
}