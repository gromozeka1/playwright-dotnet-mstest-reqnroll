using HardPoint.Testing.Driver;
using HardPoint.Testing.SmokeTests.Pages;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reqnroll;

namespace HardPoint.Testing.SmokeTests.Steps;

[Binding]
public sealed class SmokeSteps(SmokePage smokePage, PlaywrightDriver playwrightDriver)
{
    // Reaching the heading at all means BaseHook.OpenApplication navigated the driver that
    // Reqnroll resolved from the package before the scenario started.
    [Given("the smoke page is open")]
    public async Task GivenTheSmokePageIsOpen() => await Assertions.Expect(smokePage.Heading).ToBeVisibleAsync();

    [When("I type {string} into the note field")]
    public async Task WhenITypeIntoTheNoteField(string note) => await smokePage.TypeNoteAsync(note);

    [When("I submit the smoke form")]
    public async Task WhenISubmitTheSmokeForm() => await smokePage.SubmitAsync();

    [Then("the page reports {string}")]
    public async Task ThenThePageReports(string text) => await Assertions.Expect(smokePage.Result).ToHaveTextAsync(text);

    [Then("the driver saves a screenshot")]
    public async Task ThenTheDriverSavesAScreenshot()
    {
        var screenshotPath = await playwrightDriver.TakeScreenshotAsPathAsync("smoke");

        Assert.IsTrue(File.Exists(screenshotPath), $"Expected a screenshot at '{screenshotPath}'.");
        Assert.IsTrue(new FileInfo(screenshotPath).Length > 0, $"The screenshot at '{screenshotPath}' is empty.");
    }
}
