# Changelog

All notable changes to `HardPoint.Testing` are recorded here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and versions follow
[Semantic Versioning](https://semver.org/spec/v2.0.0.html). Any change to a binding
signature, the configuration shape or the tag vocabulary is at least a minor bump.

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
