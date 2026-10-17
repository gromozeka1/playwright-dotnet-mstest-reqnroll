using System.Text.Json;
using Framework.Driver;
using Microsoft.Playwright;
using Tests.Fixtures;
using Tests.Models;
using Tests.Pages;

namespace Tests.UiTests;

[TestClass]
public class CreateProductTest : TestBase
{
    private IProductListPage _productListPage = null!;
    private IProductPage _productPage = null!;
    private string? _createdProductName;

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [TestInitialize]
    public async Task InitializeAsync()
    {
        // Pages read the driver's page eagerly, so create it before resolving them.
        _ = await Driver.Page;
        _productListPage = new ProductListPage(Driver);
        _productPage = new ProductPage(Driver);
    }

    // Runs before the base cleanup, so the driver is still alive; the base scope disposal always follows.
    [TestCleanup]
    public async Task DeleteCreatedProductAsync()
    {
        if (_createdProductName is null)
        {
            return;
        }

        IAPIRequestContext api = await Driver.ApiRequestContext;
        IAPIResponse listResponse = await api.GetAsync("products");
        JsonElement? data = await listResponse.JsonAsync();
        IEnumerable<Product> products = data?.Deserialize<List<Product>>(_jsonSerializerOptions) ?? [];

        foreach (Product match in products.Where(p => p.Name == _createdProductName))
        {
            _ = await api.DeleteAsync($"products/{match.Id}");
        }
    }

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
        _createdProductName = product.Name;

        await NavigateToUrl();
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