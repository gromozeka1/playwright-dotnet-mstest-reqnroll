using System.Text.Json;
using AwesomeAssertions;
using Framework.Driver;
using Microsoft.Playwright;
using Tests.Models;

namespace Tests.ApiTests;

[TestClass]
public class ProductsApiTests
{
    private readonly PlaywrightDriver _playwrightDriver = new();

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [TestCleanup]
    public void Cleanup() => _playwrightDriver.Dispose();

    [TestMethod]
    public async Task GetProductsAsync()
    {
        IAPIResponse productResponse = await (await _playwrightDriver.ApiRequestContext).GetAsync("products/1");
        _ = productResponse.Status.Should().Be(200);

        JsonElement? data = await productResponse.JsonAsync();
        Product? product = data?.Deserialize<Product>(_jsonSerializerOptions);

        _ = product.Should().NotBeNull();
        _ = product!.Name.Should().Be("Ryzen 7 7800X3D");
        _ = product.Price.Should().Be(399);
        _ = product.Id.Should().Be(1);
    }

    [TestMethod]
    public async Task DeleteProductAsync()
    {
        IAPIRequestContext apiRequestContext = await _playwrightDriver.ApiRequestContext;
        Product? product = null;

        try
        {
            product = await CreateTestProductAsync(apiRequestContext);

            IAPIResponse deleteResponse = await apiRequestContext.DeleteAsync($"products/{product.Id}");
            _ = deleteResponse.Status.Should().Be(204);

            IAPIResponse getResponse = await apiRequestContext.GetAsync($"products/{product.Id}");
            _ = getResponse.Status.Should().Be(404);
        }
        finally
        {
            if (product is not null)
            {
                IAPIResponse cleanupResponse = await apiRequestContext.DeleteAsync($"products/{product.Id}");
                _ = cleanupResponse.Status.Should().BeOneOf(204, 404);
            }
        }
    }

    private async Task<Product> CreateTestProductAsync(IAPIRequestContext apiRequestContext)
    {
        IAPIResponse response = await apiRequestContext.PostAsync("products", new APIRequestContextOptions
        {
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            },
            Data = JsonSerializer.Serialize(new
            {
                Name = $"Test product {Guid.NewGuid():N}",
                Description = "Created by ProductsApiTests",
                ProductType = (int)ProductType.CPU,
                Price = 10
            })
        });

        _ = response.Status.Should().Be(201);
        JsonElement? data = await response.JsonAsync();

        return data?.Deserialize<Product>(_jsonSerializerOptions)
            ?? throw new JsonException("The Product API returned an empty created product response.");
    }
}
