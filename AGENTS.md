# Repository Guidelines & Agent Instructions

## Solution Architecture & Module Organization

This repository builds the **HardPoint.Testing** NuGet package: a .NET 10 solution
(`TestAutomation.slnx`) pinned by `global.json`, with two projects.

| Project | Type | Description |
|---|---|---|
| `HardPoint.Testing/` | Class Library (packable) | The package: `PlaywrightDriver`, `BasePage`, `BaseHook`, `ConfigReader`/`TestSettings`, `ExtentReportManager`, `ArtifactDirectory`, `ScenarioTags`. Namespaces are `HardPoint.Testing.*`. |
| `HardPoint.Testing.Tests/` | MSTest | Unit tests for configuration reading and artifact-path resolution. |

### Dependency Flow & Isolation

- `HardPoint.Testing.Tests` references `HardPoint.Testing`.
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

There is no end-to-end suite here: `dotnet pack` plus the unit tests are the local
verification. Any behaviour that needs a browser is proven in the consuming suite.

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
- **Settings.** `browserType` and `applicationUrl` are required; `enforceScenarioTags`
  defaults to `false` so the package imposes no tagging policy; `outputDirectory` routes the
  report, traces, screenshots and HAR files (empty means the test output directory).
- **Parallelism belongs to the consumer.** `[assembly: Parallelize]` must be declared in the
  consuming test assembly; the package cannot do it for them.
- **Artifacts.** `TestReports.html` at the artifact root, `traces/` per failed `@ui`
  scenario, `screenshots/` for report attachments, `sut-*.har` per browser context.

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
- Do not commit `bin/`, `obj/`, `*.nupkg`, `*.snupkg`, `artifacts/`, or generated reports.
