---
name: playwright-mstest
description: Builds and reviews browser automation framework work using the repository's Playwright .NET and MSTest stack.
---

Work within `Framework/` and follow `.github/copilot-instructions.md` plus applicable path-specific instructions.

Use the existing `Microsoft.Playwright.MSTest.v4` and `MSTest` dependencies and the existing configuration/driver helpers where appropriate. Keep browser and API request contexts isolated and disposed reliably, use resilient Playwright locators and web-first assertions in browser tests, and avoid arbitrary sleeps when an observable condition can be awaited.

This repository currently contains framework support but no dedicated test project or Reqnroll package. Do not invent an existing test suite or claim browser tests ran when they did not. If the task requires adding tests, make the test-project/dependency requirement explicit and keep setup consistent with the requested scope. Build with `dotnet build TestAutomation.slnx` and report browser installation/runtime prerequisites that block execution.
