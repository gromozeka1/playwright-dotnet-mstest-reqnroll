using HardPoint.Testing.Driver;
using Reqnroll;

namespace HardPoint.Testing.SmokeTests.Hooks;

// BaseHook is abstract and Reqnroll skips abstract bindings, so a consumer derives a concrete
// hooks class to get the tag check, the report node, the page navigation and the artifact capture.
[Binding]
public class SmokeHooks : BaseHook
{
    [BeforeTestRun]
    public static void OpenTheLocalPage()
    {
        // SmokePage.html ships next to the test assembly and is opened over file://, so the scenario
        // needs no web server. TEST_ApplicationUrl is the package's documented override, which is why
        // appsettings.json can keep an unreachable placeholder URL: if the override stops working the
        // scenario fails on a refused connection instead of passing against nothing.
        var pagePath = Path.Combine(AppContext.BaseDirectory, "SmokePage.html");

        Environment.SetEnvironmentVariable("TEST_ApplicationUrl", new Uri(pagePath).AbsoluteUri);
    }
}
