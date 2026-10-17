using Framework.Config;
using Framework.Driver;
using Reqnroll.BoDi;
using Tests.Pages;
using Tests.Support;

namespace Tests.Hooks;

[Binding]
public class Dependencies(IObjectContainer container)
{
    // Runs before other hooks so they can resolve the interfaces below; the container disposes the driver per scenario.
    [BeforeScenario(Order = 0)]
    public void Register()
    {
        container.RegisterInstanceAs(ConfigReader.Settings);
        container.RegisterTypeAs<PlaywrightDriver, IPlaywrightDriver>();
        container.RegisterTypeAs<ProductListPage, IProductListPage>();
        container.RegisterTypeAs<ProductPage, IProductPage>();
    }
}
