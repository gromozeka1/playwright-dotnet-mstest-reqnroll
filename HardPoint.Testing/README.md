# HardPoint.Testing

Playwright, MSTest and Reqnroll building blocks for a browser and API test suite:
a per-scenario driver, JSON configuration with environment overrides, ExtentReports
integration, and scenario hooks that open the application and capture failure artifacts.

## Install

```sh
dotnet add package HardPoint.Testing --prerelease
pwsh bin/Debug/net10.0/playwright.ps1 install chromium
```

The package depends on `Microsoft.Playwright`, `Reqnroll.MSTest` and `ExtentReports`.
Target framework is `net10.0`.

## What it provides

| Type | Purpose |
|---|---|
| `PlaywrightDriver` | One instance per scenario, resolved by Reqnroll's DI container. Exposes asynchronous `GetPageAsync()`, `Browser`, `BrowserContext`, `ApiRequestContext`, tracing and screenshots. |
| `BaseHook` | `[Binding]` that validates scenario tags, starts the Extent report node, opens the application for `@ui` scenarios, records failure traces and screenshots, and disposes the report and driver after the run. Derive from it in your own hooks class. |
| `BasePage` | Base class for page objects with asynchronous ARIA-role locator and click helpers. |
| `ConfigReader` | Reads `TestSettings` from JSON, with `TEST_`-prefixed environment overrides. |
| `ScenarioTags` | The `@ui` / `@api` test-type tags and the `@local` / `@dev` / `@qa` / `@stage` / `@prod` environment tags. |
| `ArtifactDirectory` | Resolves where the report, traces, screenshots and HAR files are written. |

## Configuration contract

The framework reads `appsettings.json` from the test output directory. `appsettings.CI.json`
is used instead when the `CI` environment variable is `true` or `1` and that file exists.
Copy both with `CopyToOutputDirectory=PreserveNewest`.

```json
{
  "browserType": "chromium",
  "applicationUrl": "http://localhost:5168",
  "enforceScenarioTags": false,
  "warnOnSerialExecution": true,
  "outputDirectory": "",
  "browserOptions": { "headless": true },
  "browserContextOptions": { "storageStatePath": "auth/state.json", "locale": "en-GB" },
  "apiOptions": { "BaseURL": "http://localhost:5044/api/", "IgnoreHTTPSErrors": true },
  "harOptions": { "recordHar": true, "urlFilter": "**/*" }
}
```

| Key | Meaning |
|---|---|
| `browserType` | `chromium`, `firefox` or `webkit`. Required, and validated when the file is read: any other value fails with the resolved path and the accepted values. Matching is case-insensitive; the canonical spelling is stored. |
| `applicationUrl` | URL opened before each `@ui` scenario. Required. |
| `enforceScenarioTags` | Reject a scenario that has no test-type tag (`@ui`/`@api`) or no environment tag, before it runs. Default `false`, so the package imposes no tagging policy unless you ask for it. |
| `warnOnSerialExecution` | Write a one-time warning when the test assembly declares no `[assembly: Parallelize]`, because the suite would otherwise run serially while still passing. Default `true`; set `false` for a deliberately serial suite. |
| `outputDirectory` | Directory for `TestReports.html`, `traces/`, `screenshots/` and `sut-*.har`. Empty means the test output directory; a relative path resolves against it. |
| `browserOptions` | Passed to `IBrowserType.LaunchAsync`. |
| `browserContextOptions` | Passed to `IBrowser.NewContextAsync`: `storageStatePath`, `extraHTTPHeaders`, `viewportSize`, `locale`, `timezoneId`, `permissions`, `baseURL`, `ignoreHTTPSErrors` and the rest of Playwright's context surface. The framework still owns the HAR path and filter (`harOptions`); with `harOptions.recordHar: false` a HAR you configured here is left alone. |
| `apiOptions` | Passed to `APIRequest.NewContextAsync`. |
| `harOptions.recordHar` | Record a HAR archive per browser context. Default `true`; `false` turns recording off. |
| `harOptions.urlFilter` | Playwright glob or regex selecting the requests written to the archive. Default `**/*`, so the archive covers the whole application rather than one suite's URLs; an empty value also records every request. |

To sign in once instead of per scenario, save Playwright's storage state and point
`browserContextOptions.storageStatePath` at it; the package passes the path through unchanged.

`TEST_`-prefixed environment variables override the file: `TEST_Headless=true` matches the
`headless` key wherever it appears, and `TEST_APIRequestNewContextOptions__BaseURL=...`
addresses a nested setting by its property path (`__` separates levels). `TEST_CONFIG_FILE`
replaces the file entirely; a relative value is resolved against the test output directory.
An override can only change a key that the file already contains.

A missing or malformed file fails with a message naming the resolved path and the remedy.

## Requirements on the consuming test assembly

- **Parallelism is not inherited from the package.** Assembly-level attributes configure the
  assembly that declares them, so declare it in your test project:

  ```csharp
  [assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]
  ```

  If the attribute is missing, `BaseHook` writes a one-time warning when the run starts rather
  than leaving the suite silently serial; `warnOnSerialExecution: false` silences it. To keep
  tag-marked features out of parallel runs, use Reqnroll's `addNonParallelizableMarkerForTags`
  in `reqnroll.json`.

- **Bindings.** `BaseHook` carries `[Binding]` and Reqnroll discovers it from the package
  reference with only `"bindingAssemblies": []` (verified with Reqnroll 3.3.4). If a future
  Reqnroll version stops scanning package assemblies, add the framework assembly:

  ```json
  { "bindingAssemblies": ["HardPoint.Testing"] }
  ```

## Artifacts

`TestReports.html` is written to the artifact directory at the end of the run. `traces/`
holds a Playwright trace archive per failed `@ui` scenario; open one with
`playwright show-trace <file>.zip`. `screenshots/` holds screenshots attached to the report.
`sut-*.har` holds one archive per browser context, unless `harOptions.recordHar` is `false`.
`ArtifactDirectory` is public if your own hooks need the same paths.
