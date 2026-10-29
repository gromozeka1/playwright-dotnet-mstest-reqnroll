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

---

## Essential Commands

- `dotnet build TestAutomation.slnx` - build both projects.
- `dotnet test HardPoint.Testing.Tests/HardPoint.Testing.Tests.csproj` - framework unit tests.
- `dotnet format TestAutomation.slnx --verify-no-changes` - formatting gate used by CI.
- `dotnet pack HardPoint.Testing/HardPoint.Testing.csproj -c Release -o artifacts` - package
  plus symbol package.
- `dotnet pack HardPoint.Testing/HardPoint.Testing.csproj -c Release -o ../localfeed` - the
  feed the consuming suite restores from until the package is on nuget.org.

There is no end-to-end suite here; behaviour that needs a browser is verified in
hardpoint-tests.

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
