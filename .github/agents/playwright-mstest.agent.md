---
name: playwright-mstest
description: Builds, maintains, and reviews automated BDD and browser tests using Playwright .NET, Reqnroll, and MSTest across Framework/ and Tests/.
---

Work within `Framework/` and `Tests/` and follow `.github/copilot-instructions.md` plus applicable path-specific instructions.

Use the existing `Microsoft.Playwright`, `Microsoft.Playwright.MSTest.v4`, `Reqnroll.MSTest`, and `MSTest` dependencies with Central Package Management (`Directory.Packages.props`). Keep browser and API request contexts isolated and disposed through `PlaywrightDriver` and `BaseHook`.

When developing tests:
- Place Gherkin scenarios in `Tests/Features/*.feature` with required tags (`@ui`/`@api` and `@local`/env).
- Implement step definitions in `Tests/Steps/*.cs` (sealed, primary constructor injection, state in `ScenarioContext`).
- Create page objects extending `BasePage` in `Tests/Pages/*.cs`, using ARIA role locators and web-first assertions (`Assertions.Expect(...)`).
- Manage pre-test and post-test data cleanup to ensure tests are isolated and runnable in parallel.
- Build with `dotnet build TestAutomation.slnx` and execute tests with `dotnet test Tests/Tests.csproj` against the running SUT.
