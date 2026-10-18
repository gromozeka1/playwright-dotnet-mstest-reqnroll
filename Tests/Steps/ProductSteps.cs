using Microsoft.Playwright;
using Reqnroll;
using Tests.Models;
using Tests.Pages;

namespace Tests.Steps;

[Binding]
public sealed class ProductSteps(ScenarioContext scenarioContext, ProductPage productPage, ProductListPage productListPage)
{
    [Given("I click the Product menu")]
    public async Task GivenIClickTheProductMenu() => await productListPage.CreateProductAsync();

    [Given("I create product with following details")]
    public async Task GivenICreateProductWithFollowingDetails(DataTable table)
    {
        Product product = table.CreateInstance<Product>();

        scenarioContext.Set(product);
        await productPage.CreateProduct(product);
        await productPage.ClickCreate();
    }

    [When("I click the Details link of the newly created product")]
    public async Task WhenIClickTheDetailsLinkOfTheNewlyCreatedProduct()
    {
        Product product = scenarioContext.Get<Product>();
        await productListPage.ClickProductFromList(product.Name);
    }

    [Then("I see all the product details are created as expected")]
    public async Task ThenISeeAllTheProductDetailsAreCreatedAsExpected()
    {
        Product product = scenarioContext.Get<Product>();
        await Assertions.Expect(productListPage.IsProductCreated(product.Name)).ToBeVisibleAsync();
    }
}
