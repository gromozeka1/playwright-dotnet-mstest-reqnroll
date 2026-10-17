using System.Text.Json;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using Framework.Driver;
using Microsoft.Playwright;
using Tests.Models;

namespace Tests.ApiTests;

[TestClass]
public class ProductsApiTests(IPlaywrightDriver playwrightDriver)
{
    private readonly IPlaywrightDriver _playwrightDriver = playwrightDriver;

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [TestMethod]
    public async Task GetProductsAsync()
    {
        IAPIResponse productResponse = await (await _playwrightDriver.ApiRequestContext).GetAsync("Product/GetProductById/1");

        JsonElement? data = await productResponse.JsonAsync();

        Product? product = data?.Deserialize<Product>(_jsonSerializerOptions);

        using (new AssertionScope())
        {
            _ = productResponse.Status.Should().Be(200);
            _ = product?.Name.Should().Be("Keyboard");
            _ = product?.Price.Should().Be(150);
            _ = product?.Id.Should().Be(1);
        }
    }

    [TestMethod]
    public async Task DeleteProductAsync()
    {
        IAPIResponse deleteResponse = await (await _playwrightDriver.ApiRequestContext).DeleteAsync("Product/Delete",
            new APIRequestContextOptions()
            {
                Params = new Dictionary<string, object>
                {
                    {"id", "1"}
                }
            });

        IAPIResponse getResponse = await (await _playwrightDriver.ApiRequestContext).GetAsync("Product/GetProductById/1");

        using (new AssertionScope())
        {
            _ = deleteResponse.Status.Should().Be(200);
            _ = getResponse.Status.Should().Be(204);
        }
    }
}
