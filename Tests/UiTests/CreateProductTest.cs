using Framework.Driver;
using Microsoft.Playwright;
using Tests.Fixtures;
using Tests.Models;
using Tests.Pages;

namespace Tests.UiTests;

[TestClass]
public class CreateProductTest
{
    private readonly PlaywrightDriver _playwrightDriver = new();
    private TestFixtureBase _testFixtureBase = null!;
    private ProductListPage _productListPage = null!;
    private ProductPage _productPage = null!;

    [TestInitialize]
    public async Task InitializeAsync()
    {
        _ = await _playwrightDriver.Page;
        _testFixtureBase = new TestFixtureBase(_playwrightDriver);
        _productListPage = new ProductListPage(_playwrightDriver);
        _productPage = new ProductPage(_playwrightDriver);
    }

    [TestCleanup]
    public void Cleanup() => _playwrightDriver.Dispose();

    [TestMethod]
    public async Task CreateProduct()
    {
        Product product = new()
        {
            Name = $"UI test product {Guid.NewGuid():N}",
            Description = "Created by the UI test",
            Price = 100,
            ProductType = ProductType.CPU
        };

        await _testFixtureBase.NavigateToUrl();
        await _productListPage.CreateProductAsync();
        await _productPage.CreateProduct(product);
        await _productPage.ClickCreate();

        // Act
        await _productListPage.ClickProductFromList(product.Name);

        // Assert
        var element = _productListPage.IsProductCreated(product.Name);
        await Assertions.Expect(element).ToBeVisibleAsync();
    }
}