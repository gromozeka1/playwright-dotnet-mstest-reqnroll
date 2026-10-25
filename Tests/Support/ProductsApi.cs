using System.Text.Json;
using Framework.Driver;
using Microsoft.Playwright;
using Tests.Models;

namespace Tests.Support;

public class ProductsApi(PlaywrightDriver playwrightDriver)
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private Task<IAPIRequestContext> Api => playwrightDriver.ApiRequestContext;

    public async Task<IAPIResponse> GetAsync(int id) => await (await Api).GetAsync($"products/{id}");

    public async Task<IAPIResponse> DeleteAsync(int id) => await (await Api).DeleteAsync($"products/{id}");

    public async Task<IAPIResponse> CreateAsync(Product product) => await (await Api).PostAsync("products", new()
    {
        Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
        Data = JsonSerializer.Serialize(new
        {
            product.Name,
            product.Description,
            product.ProductType,
            product.Price
        })
    });

    public static async Task<Product> ReadAsync(IAPIResponse response)
    {
        JsonElement? data = await response.JsonAsync();

        return data?.Deserialize<Product>(_jsonSerializerOptions)
            ?? throw new JsonException("The Product API returned an empty product response.");
    }

    public async Task DeleteByNameAsync(string name)
    {
        IAPIResponse listResponse = await (await Api).GetAsync("products");
        JsonElement? data = await listResponse.JsonAsync();
        IEnumerable<Product> products = data?.Deserialize<List<Product>>(_jsonSerializerOptions) ?? [];

        foreach (Product match in products.Where(p => p.Name == name))
        {
            IAPIResponse deleteResponse = await DeleteAsync(match.Id);
            if (deleteResponse.Status is not (204 or 404))
            {
                throw new InvalidOperationException($"Deleting product '{name}' failed with HTTP {deleteResponse.Status}.");
            }
        }
    }
}
