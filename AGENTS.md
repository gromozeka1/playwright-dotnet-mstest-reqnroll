# Repository Guidelines & Agent Instructions

## Solution Architecture & Module Organization

This repository builds the **HardPoint.Testing** NuGet package: a .NET 10 solution
(`TestAutomation.slnx`) pinned by `global.json`, with two projects in the solution and a third
that is built on its own.

| Project | Type | Description |
|---|---|---|
| `HardPoint.Testing/` | Class Library (packable) | The package: `PlaywrightDriver`, `BasePage`, `BaseHook`, `ConfigReader`/`TestSettings`, `ExtentReportManager`, `ArtifactDirectory`, `ScenarioTags`. Namespaces are `HardPoint.Testing.*`. |
| `HardPoint.Testing.Tests/` | MSTest | Unit tests for configuration, artifact paths, the driver's context options, the hook guards and the report |
| `HardPoint.Testing.SmokeTests/` | MSTest + Reqnroll, not in `TestAutomation.slnx` | One Playwright scenario run against the packed package restored from `artifacts-smoke/`. |

### Dependency Flow & Isolation

- `HardPoint.Testing.Tests` references `HardPoint.Testing`.
- `HardPoint.Testing.SmokeTests` references the **package**, not the project, so it must be
  restored after `dotnet pack -o artifacts-smoke`. It declares only the test host
  (`Microsoft.NET.Test.Sdk`, `MSTest`, `Reqnroll.MSTest`, `Reqnroll.Tools.MsBuild.Generation`);
  `Microsoft.Playwright` and `ExtentReports` have to arrive through `HardPoint.Testing` — that is
  the point of the project.
- The package depends on `Microsoft.Playwright`, `Reqnroll.MSTest` and `ExtentReports`;
  those are its public dependency surface.
- The test suite and the SUT are **not** in this repository. They live in
  [hardpoint-tests](https://github.com/gromozeka1/hardpoint-tests) and
  [sut-hardpoint](https://github.com/gromozeka1/sut-hardpoint).

---

## Build, Test, and Pack

Always run commands from the repository root using the .NET 10 SDK:

```sh
# Build everything
dotnet build TestAutomation.slnx

# Framework unit tests
dotnet test HardPoint.Testing.Tests/HardPoint.Testing.Tests.csproj

# Formatting gate used by CI
dotnet format TestAutomation.slnx --verify-no-changes

# Produce the package and its symbol package
dotnet pack HardPoint.Testing/HardPoint.Testing.csproj -c Release -o artifacts
```

Framework changes reach the suite without a release: `dotnet test Tests/Tests.csproj
-p:UseLocalFramework=true` in `../hardpoint-tests` builds against these sources.

The browser-level check is the smoke project, and it is not in the solution because it needs the
feed:

```sh
dotnet pack HardPoint.Testing/HardPoint.Testing.csproj -c Release -p:Version=0.0.0-smoke -o artifacts-smoke
dotnet test HardPoint.Testing.SmokeTests/HardPoint.Testing.SmokeTests.csproj -c Release
```

CI runs it in `.github/workflows/smoke.yml` on every push and pull request, and again before
`publish.yml` pushes anything to nuget.org. Keep it covering the package surface, not the suite's
scenarios: the full behavioural suite still lives in `../hardpoint-tests`.

`dotnet.yml` also enforces a 55% line-coverage floor (`--collect:"XPlat Code Coverage"`) and a
`dotnet list package --vulnerable --include-transitive` scan; `browser-matrix.yml` runs the smoke
scenario weekly on chromium, firefox and webkit; Dependabot keeps the NuGet and Actions pins
current. A change that drops coverage below the floor fails CI — raise the floor when a gap closes.

---

## Package & Dependency Management

- **Central Package Management (CPM)** is enforced via `Directory.Packages.props`
  (`CentralPackageTransitivePinningEnabled = true`).
- **Never add a `Version` attribute** to a `PackageReference` in individual `.csproj` files.
- To add or upgrade a package: add or update `<PackageVersion Include="..." Version="..." />`
  in `Directory.Packages.props`, then reference it unpackaged in the target `.csproj`.
- Package metadata (`PackageId`, `Version`, `PackageLicenseExpression`, readme, repository)
  lives in `HardPoint.Testing/HardPoint.Testing.csproj`. `HardPoint.Testing/README.md` is
  packed as the package readme — treat it as consumer documentation.
- **Packaging hygiene.** The package ships XML documentation and SourceLink (commit stamped in the
  nuspec/PDB), and `dotnet pack` runs package validation against `PackageValidationBaselineVersion`
  (currently `0.1.0-preview.2`) — a removed or changed public member fails the pack. Bump the
  baseline when a new version is published, and document every public member (CS1591 is an error).

---

## The Package Contract

These are public API; changing them affects every consumer:

- **Bindings.** `BaseHook` is a `[Binding]` with the mandatory-tag guard, report node,
  `@ui` navigation, failure trace and screenshot, and `[AfterTestRun]` disposal. Reqnroll
  discovers it from the package reference — verified against the suite.
- **Configuration.** `appsettings.json` (or `appsettings.CI.json` when `CI` is `true`/`1`)
  in the test output directory; `TEST_`-prefixed overrides only change keys the file already
  contains; `TEST_CONFIG_FILE` replaces the file. A missing or malformed file must fail with
  a message naming the path and the remedy.
- **Settings.** `browserType` and `applicationUrl` are required; `browserType` is validated when
  the file is read (`chromium`, `firefox` or `webkit`, case-insensitive, stored canonically) so a
  typo never reaches the driver mid-scenario; `enforceScenarioTags`
  defaults to `false` so the package imposes no tagging policy; `warnOnSerialExecution`
  (default `true`) writes a one-time warning when the test assembly declares no
  `[assembly: Parallelize]`; `trace` is `off`, `on` or `retain-on-failure` (default);
  `outputDirectory` routes the
  report, traces, screenshots and HAR files (empty means the test output directory);
  `browserContextOptions` is passed to `IBrowser.NewContextAsync` (storage state, headers,
  viewport, locale, time zone, permissions, base URL…), copied per context;
  `harOptions.recordHar` (default `true`) turns HAR recording off and `harOptions.urlFilter`
  (default `**/*`) selects the recorded requests — the package carries no suite-specific URL.
- **Parallelism belongs to the consumer.** `[assembly: Parallelize]` must be declared in the
  consuming test assembly; the package cannot do it for them, but it warns once when it is
  missing.
- **Artifacts.** `TestReports.html` at the artifact root, `traces/` per failed `@ui`
  scenario (or every scenario under `trace: on`), `screenshots/` for report attachments,
  `sut-*.har` per browser context (skipped when `harOptions.recordHar` is `false`). A failed
  scenario's report node names its trace and HAR.

---

## Coding Style & Standards

Follow `.editorconfig` across all files:
- **C#**: 4 spaces indentation, file-scoped namespaces (`namespace Foo.Bar;`), LF line endings,
  nullable reference types enabled, implicit usings enabled, braces on new lines.
- **Naming**: `PascalCase` for types and public members, `_camelCase` for private fields,
  `I`-prefix for interfaces.
- **Markdown / Config**: 2 spaces indentation, LF line endings, UTF-8.

Unit tests that touch `ConfigReader` mutate process-wide state (cached settings and
`TEST_` environment variables), so they carry `[DoNotParallelize]` and restore every variable
in `[TestCleanup]`.

---

## Versioning

Prereleases (`0.1.0-preview.N`) until the consumer proves the API. Any binding signature,
config-shape or tag-vocabulary change is at least a minor bump; the consuming repository pins
the version in its `Directory.Packages.props`.

---

## Commit Guidelines

- Use imperative, concise commit messages.
- Do not commit `bin/`, `obj/`, `*.nupkg`, `*.snupkg`, `artifacts/`, `artifacts-smoke/`, or
  generated reports.
