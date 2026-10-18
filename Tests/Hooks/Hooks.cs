using Framework.Driver;
using Reqnroll;
using Tests.Models;
using Tests.Support;

namespace Tests.Hooks;

[Binding]
public class Hooks(ProductsApi productsApi) : BaseHook()
{
    [AfterScenario]
    public async Task DeleteCreatedProduct(ScenarioContext scenarioContext)
    {
        if (scenarioContext.TryGetValue(out Product product))
        {
            await productsApi.DeleteByNameAsync(product.Name);
        }
    }
}
