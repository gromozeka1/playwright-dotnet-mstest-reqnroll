using HardPoint.Testing.Driver;
using Microsoft.Playwright;

namespace HardPoint.Testing.SmokeTests.Pages;

public class SmokePage(PlaywrightDriver playwrightDriver) : BasePage(playwrightDriver)
{
    public ILocator Heading => Page.GetByRole(AriaRole.Heading, new() { Name = "HardPoint.Testing smoke page" });

    // The live region reports how many times the form was submitted.
    public ILocator Result => Page.GetByRole(AriaRole.Status);

    public async Task TypeNoteAsync(string note) => await Page.GetByLabel("Note").FillAsync(note);

    public async Task SubmitAsync() => await ClickButtonAsync("Submit");
}
