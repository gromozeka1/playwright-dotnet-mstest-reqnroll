using HardPoint.Testing.Driver;
using Microsoft.Playwright;

namespace HardPoint.Testing.SmokeTests.Pages;

public class SmokePage(PlaywrightDriver playwrightDriver) : BasePage(playwrightDriver)
{
    public async Task<ILocator> HeadingAsync()
        => (await GetPageAsync()).GetByRole(AriaRole.Heading, new() { Name = "HardPoint.Testing smoke page" });

    // The live region reports how many times the form was submitted.
    public async Task<ILocator> ResultAsync()
        => (await GetPageAsync()).GetByRole(AriaRole.Status);

    public async Task TypeNoteAsync(string note)
        => await (await GetPageAsync()).GetByLabel("Note").FillAsync(note);

    public async Task SubmitAsync() => await ClickButtonAsync("Submit");
}
