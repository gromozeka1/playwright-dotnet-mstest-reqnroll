---
name: playwright-mstest
description: Use when changing browser automation, Playwright driver lifecycle/configuration, or MSTest-based browser tests in the Framework project.
---

# Playwright .NET and MSTest

- Inspect `Framework/Driver/PlaywrightDriver.cs`, its interfaces, and `Framework/Config/` before changing browser lifecycle or test settings.
- Reuse the installed Playwright .NET/MSTest integration; preserve the configured browser selection, timeout, headless, and slow-motion behavior unless the task changes those requirements.
- In browser tests, prefer accessible locators, auto-waiting actions, and web-first assertions over fixed delays or brittle selectors. Isolate per-test browser contexts and dispose browser/API resources reliably.
- Keep screenshots and other test artifacts out of tracked source unless explicitly requested. Avoid machine-specific absolute paths.
- There is currently no dedicated test project or Reqnroll dependency. Do not imply one exists; if one is requested, establish the appropriate test project and runner explicitly.
- Build with `dotnet build TestAutomation.slnx`; only report tests as passing when they were executed.
