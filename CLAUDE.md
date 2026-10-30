# CLAUDE.md - Assistant Guidelines

## Solution Overview

This repository builds the **HardPoint.Testing** NuGet package: Playwright, MSTest and
Reqnroll building blocks for a browser and API test suite. The suite that consumes the package
lives in [hardpoint-tests](https://github.com/gromozeka1/hardpoint-tests); the system under
test lives in [sut-hardpoint](https://github.com/gromozeka1/sut-hardpoint). Neither is part of
this repository.

### Projects

- `HardPoint.Testing/`: the packable library — driver lifecycle (`PlaywrightDriver`), base
  classes (`BasePage`, `BaseHook`), configuration (`ConfigReader`, `TestSettings`), Extent
  reporting (`ExtentReportManager`), artifact paths (`ArtifactDirectory`), tag vocabulary
  (`ScenarioTags`). Namespaces are `HardPoint.Testing.*`.
- `HardPoint.Testing.Tests/`: unit tests for configuration and artifact-path resolution.
- `HardPoint.Testing.SmokeTests/`: one Playwright scenario run against the packed package restored
  from `artifacts-smoke/`. Not in `TestAutomation.slnx`, because it needs the feed.

---

## Essential Commands

- `dotnet build TestAutomation.slnx` - build both projects.
- `dotnet test HardPoint.Testing.Tests/HardPoint.Testing.Tests.csproj` - framework unit tests.
- `dotnet format TestAutomation.slnx --verify-no-changes` - formatting gate used by CI.
- `dotnet pack HardPoint.Testing/HardPoint.Testing.csproj -c Release -o artifacts` - package
  plus symbol package.
- `dotnet test Tests/Tests.csproj -p:UseLocalFramework=true` in `../hardpoint-tests` - build
  the suite against these sources without releasing; it restores from nuget.org otherwise.
- `dotnet pack HardPoint.Testing/HardPoint.Testing.csproj -c Release -p:Version=0.0.0-smoke -o artifacts-smoke`
  then `dotnet test HardPoint.Testing.SmokeTests/HardPoint.Testing.SmokeTests.csproj -c Release` -
  the browser-level check of the packed package. CI runs it on every push and before publishing.

---

## Key Development Rules

### Central Package Management (CPM)

- Versions are managed in `Directory.Packages.props`.
- **Never** add a `Version` attribute to a `<PackageReference>` in a `.csproj`.

### Public API

`BaseHook`, `BasePage`, `PlaywrightDriver`, `ConfigReader`, `TestSettings`, `ExtentReportManager`,
`ArtifactDirectory` and `ScenarioTags` are consumer-facing. A change to a binding signature, a
config key or the tag vocabulary is at least a minor version bump, and
`HardPoint.Testing/README.md` — which is packed as the package readme — must be updated with it.

### C# & Code Style (.editorconfig)

- 4 spaces for C#, 2 spaces for JSON/Markdown/YAML; LF line endings.
- File-scoped namespaces; nullable reference types and implicit usings enabled.
- Primary constructors where suitable.

### Tests

Unit tests that touch `ConfigReader` change process-wide state (cached settings, `TEST_`
environment variables): mark them `[DoNotParallelize]` and restore every variable in
`[TestCleanup]`.

See `AGENTS.md` for the full guidance.
