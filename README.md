# playwright-dotnet-mstest-reqnroll

Source of the **HardPoint.Testing** NuGet package: Playwright, MSTest and Reqnroll
building blocks for a browser and API test suite. The package provides a per-scenario
driver, JSON configuration with environment overrides, ExtentReports integration, and
scenario hooks that open the application and capture failure artifacts.

The suite that consumes it lives in the separate
[hardpoint-tests](https://github.com/gromozeka1/hardpoint-tests) repository, and the
system under test in [sut-hardpoint](https://github.com/gromozeka1/sut-hardpoint).

## Repository structure

| Path | Contents |
|---|---|
| `HardPoint.Testing/` | The package: config reader, driver, `BasePage`, `BaseHook`, Extent reporting, artifact paths, scenario tags. `README.md` here is packed into the package. |
| `HardPoint.Testing.Tests/` | Unit tests for the framework (configuration and artifact-path resolution). |
| `TestAutomation.slnx` | Both projects, pinned to .NET 10 by `global.json`. |

## Build, test, pack

```sh
dotnet build TestAutomation.slnx
dotnet test HardPoint.Testing.Tests/HardPoint.Testing.Tests.csproj
dotnet pack HardPoint.Testing/HardPoint.Testing.csproj -c Release -o artifacts
```

`dotnet pack` produces `HardPoint.Testing.<version>.nupkg` plus a `.snupkg` symbol package,
with the readme, licence expression, repository link and the three dependencies
(`Microsoft.Playwright`, `Reqnroll.MSTest`, `ExtentReports`).

### Working against the consuming suite

Pack into the folder the test repository reads as its local feed, then bump the pinned
version there:

```sh
dotnet pack HardPoint.Testing/HardPoint.Testing.csproj -c Release -o ../localfeed
```

## The package contract

`HardPoint.Testing/README.md` is the consumer-facing documentation and is packed alongside
the assembly: configuration keys (`browserType`, `applicationUrl`, `enforceScenarioTags`,
`outputDirectory`, `browserOptions`, `apiOptions`), the `TEST_` environment-override rules,
and the two things a consumer must do itself — declare `[assembly: Parallelize]` and, only if
Reqnroll fails to discover it, list the assembly in `bindingAssemblies`. Binding discovery
from the package has been verified against the suite, so no extra configuration is needed
today.

Versions are prereleases (`0.1.0-preview.1`) until the consumer migration proves the API.
Any change to a binding signature, the config shape or the tag vocabulary is at least a
minor bump.

## Commit guidelines

- Imperative, concise commit messages.
- Do not commit `bin/`, `obj/`, `*.nupkg`, `*.snupkg` or generated reports.
