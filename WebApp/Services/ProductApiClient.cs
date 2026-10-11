using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using WebApp.Models;

namespace WebApp.Services;

public class ProductApiClient
{
    private const string ProductsEndpoint = "api/products";
    private readonly HttpClient _httpClient;

    public ProductApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<ProductViewModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(ProductsEndpoint, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<ProductViewModel>>(cancellationToken: cancellationToken)
            ?? throw new JsonException("The Product API returned an empty product list response.");
    }

    public async Task<ProductViewModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync($"{ProductsEndpoint}/{id}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductViewModel>(cancellationToken: cancellationToken)
            ?? throw new JsonException("The Product API returned an empty product response.");
    }

    public async Task CreateAsync(ProductViewModel product, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(ProductsEndpoint, product, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<bool> UpdateAsync(ProductViewModel product, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsJsonAsync(
            $"{ProductsEndpoint}/{product.Id}",
            product,
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.DeleteAsync($"{ProductsEndpoint}/{id}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();
        return true;
    }
}
