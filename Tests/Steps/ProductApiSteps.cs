using AwesomeAssertions;
using Microsoft.Playwright;
using Reqnroll;
using Tests.Models;
using Tests.Support;

namespace Tests.Steps;

[Binding]
public sealed class ProductApiSteps(ScenarioContext scenarioContext, ProductsApi productsApi)
{
    private IAPIResponse _response = null!;

    [Given("a product exists with the following details")]
    public async Task GivenAProductExists(DataTable table)
    {
        Product product = table.CreateInstance<Product>();
        await productsApi.DeleteByNameAsync(product.Name);

        IAPIResponse response = await productsApi.CreateAsync(product);
        _ = response.Status.Should().Be(201);

        scenarioContext.Set(await ProductsApi.ReadAsync(response));
    }

    [When("I request the product with id {int}")]
    public async Task WhenIRequestTheProductWithId(int id) => _response = await productsApi.GetAsync(id);

    [When("I request the created product")]
    public async Task WhenIRequestTheCreatedProduct() => _response = await productsApi.GetAsync(scenarioContext.Get<Product>().Id);

    [When("I delete the created product")]
    public async Task WhenIDeleteTheCreatedProduct() => _response = await productsApi.DeleteAsync(scenarioContext.Get<Product>().Id);

    [Then("the response status is {int}")]
    public void ThenTheResponseStatusIs(int status) => _ = _response.Status.Should().Be(status);

    [Then("the product has name {string} and price {int}")]
    public async Task ThenTheProductHasNameAndPrice(string name, int price)
    {
        Product product = await ProductsApi.ReadAsync(_response);

        _ = product.Name.Should().Be(name);
        _ = product.Price.Should().Be(price);
    }
}
