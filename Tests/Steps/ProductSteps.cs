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
        var product = table.CreateInstance<Product>();

        scenarioContext.Set(product);
        await productPage.CreateProduct(product);
        await productPage.ClickCreate();
    }

    [When("I click the Details link of the newly created product")]
    public async Task WhenIClickTheDetailsLinkOfTheNewlyCreatedProduct()
        => await productListPage.ClickProductFromList(scenarioContext.Get<Product>().Name);

    [Then("I see all the product details are created as expected")]
    public async Task ThenISeeAllTheProductDetailsAreCreatedAsExpected()
        => await Assertions.Expect(productListPage.IsProductCreated(scenarioContext.Get<Product>().Name)).ToBeVisibleAsync();
}
