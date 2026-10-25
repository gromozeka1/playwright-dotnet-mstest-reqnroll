---
name: playwright-mstest
description: Use when authoring, modifying, or debugging browser automation, Playwright driver lifecycle/configuration, Reqnroll BDD scenarios, or MSTest tests across Framework and Tests.
---

# Playwright .NET, Reqnroll, and MSTest

- Inspect `Framework/Driver/PlaywrightDriver.cs`, `Framework/Driver/BasePage.cs`, and `Framework/Driver/BaseHook.cs` before changing test infrastructure or driver lifecycles.
- Respect Central Package Management: define package versions in `Directory.Packages.props`, never in individual `.csproj` files.
- In browser tests, prefer accessible locators (`GetByRole`, `GetByLabel`, `BasePage` helpers), auto-waiting actions, and web-first assertions (`Assertions.Expect(...)`) over fixed delays or brittle CSS/XPath selectors.
- Tag every scenario with mandatory test-type (`@ui` or `@api`) and environment (`@local`, `@dev`, etc.) tags to satisfy `BaseHook.VerifyMandatoryScenarioTags`.
- Ensure scenario independence: use Background cleanup steps and `[AfterScenario]` deletion hooks to keep the SUT database clean.
- Keep screenshots, HAR recordings, and test report artifacts (`TestReports.html`, `allure-results/`) out of tracked source.
- Build with `dotnet build TestAutomation.slnx`; execute tests against the running local/Compose SUT using `dotnet test Tests/Tests.csproj`.
