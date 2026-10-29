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

Iterate on the framework without releasing: the suite builds against these sources when asked
for it.

```sh
# in ../hardpoint-tests
dotnet test Tests/Tests.csproj -p:UseLocalFramework=true
```

That switch swaps the suite's package reference for a project reference to
`../playwright-dotnet-mstest-reqnroll/HardPoint.Testing/HardPoint.Testing.csproj`; keep it off
for CI and for checks that must prove package behaviour. Otherwise the suite restores the
published version pinned in its `Directory.Packages.props`, so bump that pin after a release.

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

## Publishing

Pushing a `v*` tag runs `.github/workflows/publish.yml`, which builds, tests, packs and pushes
the package and its symbols to nuget.org. Publishing uses Trusted Publishing: the workflow
exchanges a GitHub OIDC token for a short-lived API key, so the only stored secret is
`NUGET_USER` (the nuget.org profile name).

The nuget.org policy must name this repository and the workflow **file name only**:

| Field | Value |
|---|---|
| Repository Owner | `gromozeka1` |
| Repository | `playwright-dotnet-mstest-reqnroll` |
| Workflow File | `publish.yml` |
| Scopes | `HardPoint.*` |

```sh
git tag v0.1.0-preview.1
git push origin v0.1.0-preview.1
```

The workflow refuses to publish when the tag does not match the `<Version>` in
`HardPoint.Testing.csproj`. A published version can only be unlisted, never deleted, so bump
the version before tagging again.

`workflow_dispatch` runs the same job without the tag check, and it does publish — use it
only for a deliberate manual release.

## Commit guidelines

- Imperative, concise commit messages.
- Do not commit `bin/`, `obj/`, `*.nupkg`, `*.snupkg` or generated reports.
