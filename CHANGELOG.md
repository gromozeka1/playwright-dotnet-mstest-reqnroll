# Changelog

All notable changes to `HardPoint.Testing` are recorded here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and versions follow
[Semantic Versioning](https://semver.org/spec/v2.0.0.html). Any change to a binding
signature, the configuration shape or the tag vocabulary is at least a minor bump.

## [0.1.0-preview.10] - 2026-11-01

### Added

- `BasePage` form helpers: `FillAsync`, `SelectAsync`, `WaitForLoadStateAsync`, and `InRowAsync` for
  an element scoped to a named row — the boilerplate every page object repeated.
- `ApiClient` over the scenario's API request context: `PostJsonAsync`, `PutJsonAsync` and
  `GetJsonAsync<T>` set the JSON body, and a non-success call attaches the request and response
  bodies to the report (`ExtentReportManager.AttachNote`).
- A compatibility matrix and a quickstart walkthrough in the packaged README.

## [0.1.0-preview.9] - 2026-11-01

### Added

- Weekly browser matrix (`browser-matrix.yml`): the packed-package smoke scenario runs on
  chromium, firefox and webkit, so the two advertised engines that were never exercised cannot rot
  unnoticed. `smoke.yml` gained a `browser` input, passed through the documented
  `TEST_BrowserType` override.
- Coverage collection in CI (`coverlet.collector`) with a 55% line floor for `HardPoint.Testing`,
  plus tests for the tag guard, idempotent driver disposal, the report's no-op artifact attachment
  and feature-node caching.
- Dependabot for NuGet and GitHub Actions, and a
  `dotnet list package --vulnerable --include-transitive` gate in `dotnet.yml`.

## [0.1.0-preview.8] - 2026-11-01

### Added

- `trace` setting with Playwright's familiar values: `off` (record nothing), `on` (always keep)
  and `retain-on-failure` (default, the previous behaviour). `off` stops paying the recording cost
  on every green run.
- A failed `@ui` scenario's report node now names its trace archive and HAR file
  (`ExtentReportManager.AttachArtifact`), so `TestReports.html` points at the artifacts instead of
  leaving them to be found in the output directory. `PlaywrightDriver.HarPath` exposes the archive
  path for consumers with a report of their own.

### Changed

- `BaseHook.SaveTraceOnFailure` honours the `trace` setting (keeping the archive for `on` or a
  failure) and names the artifacts in the report. Its signature is unchanged.

## [0.1.0-preview.7] - 2026-11-01

### Added

- The package ships XML documentation for its public API (`GenerateDocumentationFile`), so
  consumers get IntelliSense and nuget.org API docs; a new public member without a doc comment is
  a build failure (CS1591 promoted to an error).
- Package validation compares the packed API with the last published release
  (`0.1.0-preview.2`): removing or changing a public member now fails `dotnet pack` with
  CP0001/CP0002 instead of shipping.
- SourceLink (`PublishRepositoryUrl`, `EmbedUntrackedSources`): the nuspec carries the source
  commit and the PDB gets source-link information, so stepping into the package navigates to the
  code for that exact version.

## [0.1.0-preview.6] - 2026-11-01

### Added

- `browserContextOptions` is passed to `IBrowser.NewContextAsync`, so a consumer can set storage
  state, extra HTTP headers, viewport, locale, time zone, permissions, base URL and the rest of
  Playwright's context surface — previously the context was built internally with the HAR path and
  nothing else, leaving storage-state authentication unreachable without reflection. Each context
  gets a copy, so parallel scenarios cannot share the mutable HAR path, and the framework still
  owns the HAR path and filter (`harOptions`); with `harOptions.recordHar: false` a HAR configured
  in `browserContextOptions` is left alone.

## [0.1.0-preview.5] - 2026-11-01

### Added

- `BaseHook` writes a one-time warning when the consuming test assembly declares no
  `[assembly: Parallelize]` attribute, so a suite that silently runs serially is visible instead
  of merely slower. The attribute is read from metadata by name, keeping the package free of a
  compile-time MSTest reference, and `warnOnSerialExecution` (default `true`) turns the warning
  off for deliberately serial suites.

## [0.1.0-preview.4] - 2026-11-01

### Fixed

- A mistyped `browserType` now fails when the configuration is read, not mid-scenario when the
  browser launches — after `BeforeScenario` had already opened the report node. The error names
  the resolved file, the `browserType` key and the accepted values (`chromium`, `firefox`,
  `webkit`). Matching is case-insensitive and the canonical spelling is stored, so the driver
  never sees an unknown value.

## [0.1.0-preview.3] - 2026-10-31

### Changed

- HAR recording no longer carries the demo suite's URL filter. `PlaywrightDriver` hard-coded
  `**/Product/**`, so a consumer testing anything else got an almost empty archive — exactly the
  artifact a failing CI run needs. The filter now comes from `harOptions.urlFilter` and defaults
  to `**/*`, which records every request; `harOptions.recordHar` (default `true`) turns recording
  off entirely. Configurations that do not mention `harOptions` keep one HAR per browser context,
  now covering the whole application.

## [0.1.0-preview.2] - 2026-10-31

### Changed

- `BaseHook` attaches a screenshot only when a `@ui` scenario fails, which is what the hook name
  and the README always said: the guard was commented out, so every scenario was captured.
- `PlaywrightDriver` teardown is asynchronous. `DisposeAsync` closes the page, browser context,
  browser, API request context and the Playwright driver in that order, replacing
  `Task.Run(...).Wait()` and the two fixed 500 ms delays; `Dispose()` stays for synchronous
  callers and blocks on the same sequence.
- Page access is asynchronous end to end: removed the synchronous `PlaywrightDriver.Page` bridge,
  made `BasePage` locator helpers asynchronous, and migrated the smoke project and suite consumer.

### Removed

- `PlaywrightDriver.Dispose(bool)` — disposal goes through `DisposeAsync()` (preferred) or
  `Dispose()`.
- `Config.DriverType` — an unused enum; the browser is chosen by the `browserType` setting.
- `PlaywrightDriver.Page` and synchronous `BasePage` locator helpers.

## [0.1.0-preview.1] - 2026-10-30

The first published prerelease. It was extracted from the single-solution repository the suite
used to share, and [hardpoint-tests](https://github.com/gromozeka1/hardpoint-tests) consumes it
from nuget.org to prove the API before 1.0.0.

### Added

- `PlaywrightDriver`: one browser and one API request context per scenario, Playwright tracing
  per `@ui` scenario (kept only when the scenario fails), screenshots, and a HAR archive per
  browser context.
- `BasePage` and `BaseHook`: page helpers (`Link`, `Button`, `Row`, `Text` and the click
  helpers), the mandatory-tag guard, a report node per scenario, navigation for `@ui`, and
  failure artifacts.
- `ConfigReader` and `TestSettings`: `appsettings.json` (or `appsettings.CI.json` when `CI` is
  `true`) from the test output directory, `TEST_`-prefixed environment overrides that only
  change keys the file already declares, and `TEST_CONFIG_FILE` to name another file. A missing
  or malformed file fails with the resolved path and the remedy rather than a
  `TypeInitializationException`.
- `ExtentReportManager` and `ArtifactDirectory`: one HTML report and one artifact root, routed
  by the `outputDirectory` setting (empty means the test output directory).
- `ScenarioTags`: the shared `@ui`/`@api` and environment tag vocabulary.

### Changed

- Tag enforcement is opt-in: `enforceScenarioTags` defaults to `false`, so the package imposes
  no tagging policy on a consumer.
- `ExtentReportManager` serialises reporter and feature-node creation behind a single lock and
  caches feature nodes by title; the report has one block per feature instead of one per
  scenario.
- Relative `outputDirectory` values resolve against the test output directory.

### Consumer checklist

- Declare `[assembly: Parallelize]` in the consuming test assembly. An assembly-level attribute
  declared in the package configures the package, not the consumer, and the tests silently run
  serially.
- Leave `bindingAssemblies` empty: Reqnroll discovers `BaseHook` from the package assembly.
- `Microsoft.Playwright` arrives transitively; run `playwright.ps1 install chromium` from the
  test output directory after the first build.
- `ExtentReports` 5.0.4 depends on `System.Drawing.Common` 5.0.0, which carries a critical
  advisory (GHSA-rxg9-xrhp-64gj). Pin `System.Drawing.Common` to a supported version in the
  consumer's central package management.

### Known limitations at release

- `PlaywrightDriver.Dispose` blocks on `Task.Run(...).Wait()`, and `PlaywrightDriver.Page` is
  `_page.Value.Result`, so the first page access blocks the calling thread.
- `BaseHook` attaches a full-page screenshot after every `@ui` scenario: the failure-only guard
  in `AttachScreenshotOnFailure` is commented out.
