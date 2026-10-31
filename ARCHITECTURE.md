# playwright-dotnet-mstest-reqnroll — architecture

This repository builds the **`HardPoint.Testing`** NuGet package: Playwright, MSTest and Reqnroll
building blocks for a browser and API test suite — a per-scenario driver, JSON configuration with
environment overrides, ExtentReports integration, and scenario hooks that open the application and
capture failure artifacts.

It contains no application and no test suite. Both live elsewhere:

| Repo | Relationship |
|---|---|
| [hardpoint-tests](https://github.com/gromozeka1/hardpoint-tests) | The single consumer today: restores this package from nuget.org and pins its version |
| [sut-hardpoint](https://github.com/gromozeka1/sut-hardpoint) | The application the consumer tests; irrelevant to this package except as the proving ground |

Start with [README.md](README.md) for commands and
[HardPoint.Testing/README.md](HardPoint.Testing/README.md) for the consumer-facing contract — the
latter is **packed into the `.nupkg`** and rendered on nuget.org, so treat it as public
documentation, not notes.

---

## 1. Projects

| Project | Type | Notes |
|---|---|---|
| `HardPoint.Testing/` | Packable class library (`net10.0`) | The package itself — 8 source files |
| `HardPoint.Testing.Tests/` | MSTest | 14 unit tests: configuration reading and artifact-path resolution |
| `HardPoint.Testing.SmokeTests/` | MSTest + Reqnroll | One real browser scenario against the **packed** package — **outside `TestAutomation.slnx`**, because it needs the `artifacts-smoke/` feed |

Solution `TestAutomation.slnx`; .NET 10 pinned by `global.json`. Dependencies:
`Microsoft.Playwright`, `Reqnroll.MSTest`, `ExtentReports` — these are the public dependency
surface, and Playwright and ExtentReports must reach consumers **transitively** (that is the point
of the smoke project).

## 2. Source layout

| File | Role |
|---|---|
| `Driver/PlaywrightDriver.cs` | One browser + one API request context per scenario; lazy creation, `GetPageAsync()`, tracing, screenshots, `DisposeAsync()` |
| `Driver/BaseHook.cs` | The `[Binding]` every consumer derives from: tag guard, report node, `@ui` navigation, failure trace/screenshot, `[AfterTestRun]` disposal |
| `Driver/BasePage.cs` | Page-object base with async ARIA-role locator and click helpers |
| `Config/ConfigReader.cs` | Reads `TestSettings` from JSON with `TEST_` environment overrides |
| `Config/TestSettings.cs` | The settings shape (Playwright launch/API-context option types) |
| `Helpers/ExtentReportManager.cs` | Thread-safe single HTML report, feature nodes cached by title |
| `Helpers/ArtifactDirectory.cs` | Single artifact root routed by `outputDirectory` |
| `Constants/ScenarioTags.cs` | `@ui`/`@api` plus the environment tag vocabulary |

## 3. What happens during one scenario

```
Reqnroll DI resolves PlaywrightDriver (one per scenario)
  │
  ├─ BeforeScenario  Order = int.MinValue  VerifyMandatoryScenarioTags   (if enforceScenarioTags)
  ├─ BeforeScenario  Order = 10            StartReportScenario           (always)
  ├─ BeforeScenario  Order = 20            OpenApplication               (@ui only → applicationUrl)
  │                                         + tracing started            (@ui only)
  ├─ AfterStep                             ReportStep                    (always)
  ├─ AfterScenario                         AttachScreenshotOnFailure     (@ui only, failure only)
  │                                         + trace kept only on failure
  └─ AfterTestRun   (static)               flush report, dispose driver
```

Resources are lazy: `@api` scenarios never launch a browser; the page and API context are created
on first use. Disposal is asynchronous and ordered — page, context, browser, API context, driver —
via `DisposeAsync()`; a synchronous `Dispose()` remains as an `IDisposable` adapter.

## 4. Configuration contract

`appsettings.json` is read from `AppContext.BaseDirectory` (the test output directory);
`appsettings.CI.json` is used instead when `CI` is `true` or `1` and that file exists. Consumers
copy both with `CopyToOutputDirectory=PreserveNewest`.

```json
{
  "browserType": "chromium",
  "applicationUrl": "http://localhost:5168",
  "enforceScenarioTags": false,
  "outputDirectory": "",
  "browserOptions": { "headless": true },
  "apiOptions": { "BaseURL": "http://localhost:5044/api/", "IgnoreHTTPSErrors": true }
}
```

| Key | Meaning |
|---|---|
| `browserType` | `chromium`, `firefox`, `webkit` — **required** |
| `applicationUrl` | Opened before each `@ui` scenario — **required** |
| `enforceScenarioTags` | Reject a scenario missing a test-type tag or environment tag before it runs. Default **`false`**: the package imposes no tagging policy unless asked |
| `outputDirectory` | Root for report/traces/screenshots/HAR. Empty = test output directory; relative resolves against it |
| `browserOptions` | Passed to `IBrowserType.LaunchAsync` |
| `apiOptions` | Passed to `APIRequest.NewContextAsync` |

Override rules — these are **external API**, unit-tested:

- `TEST_<Key>` overrides a setting, matching the property name case-insensitively, wherever the
  key appears (`TEST_Headless=true`).
- Nested values use a `__`-separated property path:
  `TEST_APIRequestNewContextOptions__BaseURL=...`.
- `TEST_CONFIG_FILE` replaces the file entirely; a relative value resolves against the test output
  directory.
- **An override can only change a key the file already contains.**
- A missing or malformed file fails with a message naming the resolved path and the remedy — never
  a bare `TypeInitializationException`.

## 5. What the package cannot do for a consumer

Both are silent-failure traps, and both are documented in the packed README because of that:

1. **Parallelism.** Assembly-level attributes configure the assembly that declares them, so
   `[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]` must be declared in the *consumer's*
   test project. Declared here, it would configure this package and consumer suites would keep
   passing while running serially.
2. **Binding discovery.** `BaseHook` carries `[Binding]` and Reqnroll discovers it from the package
   reference with `"bindingAssemblies": []` (verified with Reqnroll 3.3.4). Only if a future Reqnroll
   stops scanning package assemblies does the consumer add `{ "bindingAssemblies": ["HardPoint.Testing"] }`.

If either breaks, the consumer loses UI navigation, tracing, screenshots and reporting **while
still going green** — which is exactly what the smoke project exists to catch.

## 6. Artifacts the package writes

| Path (under the artifact root) | Contents |
|---|---|
| `TestReports.html` | The Extent HTML report, one block per feature |
| `traces/` | `{scenario-title}-{id}.zip` per **failed** `@ui` scenario; passing traces are deleted |
| `screenshots/` | Screenshots attached to report steps |
| `sut-*.har` | One HAR archive per browser context |

`ArtifactDirectory` is public so consumer hooks can resolve the same paths.

## 7. Dev loop with the consumer

Changes can be proven without publishing:

```sh
# in ../hardpoint-tests
dotnet test Tests/Tests.csproj -p:UseLocalFramework=true
```

That swaps the consumer's `PackageReference` for a `ProjectReference` to
`../playwright-dotnet-mstest-reqnroll/HardPoint.Testing/HardPoint.Testing.csproj`. Keep it off for
CI and for any check that must prove package behaviour. Otherwise the consumer restores the
published version pinned in **its** `Directory.Packages.props` — bump that pin after a release.

## 8. Verification layers

| Layer | Command | Proves |
|---|---|---|
| Unit tests | `dotnet test HardPoint.Testing.Tests/HardPoint.Testing.Tests.csproj` | `ConfigReader` and `ArtifactDirectory`, including failure messages and override limits |
| Formatting gate | `dotnet format TestAutomation.slnx --verify-no-changes` | Style is CI-enforced |
| Package smoke | `dotnet pack ... -p:Version=0.0.0-smoke -o artifacts-smoke` then `dotnet test HardPoint.Testing.SmokeTests/... -c Release` | **The `.nupkg` itself**: dependency completeness, driver launch, hook execution, report written |
| Real consumer | the suite's 20 scenarios against the Docker SUT | End-to-end package behaviour |

`0.0.0-smoke` is local-only so the restore can only resolve the package just packed. `CI=true`
selects `appsettings.CI.json` and runs headless, exactly as a consumer does; a local run is headed.
**Pack first** — NuGet fails a restore outright when a configured folder source does not exist.

The smoke project must keep covering the *package surface*, not grow into a second suite.

## 9. Packaging and versioning

`dotnet pack HardPoint.Testing/HardPoint.Testing.csproj -c Release -o artifacts` produces
`HardPoint.Testing.<version>.nupkg` plus a `.snupkg`, with the readme, licence expression
(MIT — no `LICENSE` file needed), repository link and the three dependencies.

- Semantic versioning, [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) format in
  [CHANGELOG.md](CHANGELOG.md).
- **Any** change to a binding signature, the configuration shape or the tag vocabulary is **at least
  a minor bump**. Patches otherwise.
- Versions stay prerelease until a consumer proves the API. Current: `0.1.0-preview.2`.
- When a config key, tag or binding changes, update `HardPoint.Testing/README.md` in the same change
  — it ships inside the package.

## 10. Publishing

Pushing a `v*` tag runs `.github/workflows/publish.yml`: build, test, run the package smoke test,
pack, then push package + symbols to nuget.org. **Publishing waits on the smoke job** — a version
can be unlisted but never deleted, so a broken package must not reach nuget.org.

- The workflow refuses a tag that does not match `<Version>` in `HardPoint.Testing.csproj`.
- `workflow_dispatch` runs the same job without the tag check **and does publish** — deliberate
  manual releases only.
- Trusted Publishing only: a GitHub OIDC token is exchanged for a short-lived key
  (`NuGet/login@v1`, `permissions: id-token: write`). `NUGET_USER` (the nuget.org profile name) is
  the only stored secret.
- The nuget.org policy must name the repository and the workflow **file name only**:

| Field | Value |
|---|---|
| Repository Owner | `gromozeka1` |
| Repository | `playwright-dotnet-mstest-reqnroll` |
| Workflow File | `publish.yml` |
| Scopes | `HardPoint.*` |

## 11. CI workflows

| Workflow | Trigger | Purpose |
|---|---|---|
| `dotnet.yml` | every push and PR | Restore, build, unit test, `dotnet format --verify-no-changes`, pack, upload |
| `smoke.yml` | every push and PR, and as a gate in `publish.yml` | Pack `0.0.0-smoke`, assert the `.nupkg` exists, restore the smoke project from it, install Chromium via the package's own `playwright.ps1`, run the scenario, assert exactly one test ran and the report contains its title |
| `publish.yml` | tag `v*` or `workflow_dispatch` | The release path in §10 |

## 12. Conventions

- **Central Package Management.** Versions in `Directory.Packages.props`
  (`CentralPackageTransitivePinningEnabled = true`). Never a `Version` attribute on a
  `PackageReference` in a `.csproj`.
- **Style** (`.editorconfig`): 4 spaces for C#, 2 for JSON/Markdown/YAML, LF endings, file-scoped
  namespaces, nullable + implicit usings enabled, braces on new lines. Primary constructors where
  suitable.
- **Unit tests that touch `ConfigReader` mutate process-wide state** (cached settings and `TEST_`
  environment variables): mark them `[DoNotParallelize]` and restore every variable in
  `[TestCleanup]`.
- **Commits**: imperative and concise. Do not commit `bin/`, `obj/`, `*.nupkg`, `*.snupkg`,
  `artifacts/`, `artifacts-smoke/` or generated reports.

## 13. Why the repositories are split

The package was extracted from a single repository that also held the suite. The migration is
finished; this section is the durable record of *why* the boundaries sit where they do. Read it
before "simplifying" any of them back into one repository.

### Decisions that shaped it

| Decision | Choice | Why |
|---|---|---|
| Separate repositories vs. monorepo | Separate repositories | Makes the package boundary real, and keeps the suite free of framework internals. A monorepo would let the package quietly depend on project internals. |
| Runner coupling | One MSTest + Reqnroll package | Splitting into core/`.MSTest`/`.Reqnroll` doubles packaging work for no benefit while there is only one runner flavour. |
| Package id | `HardPoint.Testing`, namespace `HardPoint.Testing.*` | `Framework` is too generic for a public id. |
| Versioning | Prereleases until a consumer proves the API | Leaves room to break the API during migration; `1.0.0` once proven. |
| Dev loop | `-p:UseLocalFramework=true` swaps in a `ProjectReference` | CI and fresh clones always consume the published package; framework work stays fast. |
| Publishing | Trusted Publishing (OIDC) | No long-lived API key; `NUGET_USER` is the only stored secret. |

### Safeguards, and the traps they prevent

Every row below was a silent-failure mode found while splitting: the build stays green and the
symptom shows up as lost coverage or a stale package. All are closed, and the smoke project is what
keeps them closed.

| Trap | Why it is silent | Prevented now by |
|---|---|---|
| Reqnroll does not discover `BaseHook` in the package assembly | `@ui` scenarios lose navigation, tracing and reporting while still passing | `bindingAssemblies: []` verified against a real consumer; the smoke project runs an actual scenario |
| `[assembly: Parallelize]` shipped inside the package | The suite passes, just serially — 5–10× slower | Declared in the consumer's `AssemblyInfo.cs`; §5 |
| Tag vocabulary defined twice | `@local` rejected by one list and accepted by another | Single-sourced in `ScenarioTags`; §4 |
| Config-file contract understood differently by consumers | First run dies with `TypeInitializationException` | Packed README plus unit tests on the failure messages; §4 |
| Version drift between the two repositories | The suite silently tests an old package | One `HardPointTestingVersion` property plus the weekly canary |

### Deliberately not in the package

Page objects, step definitions, feature files, `docker-compose.yml`, `appsettings*.json` and
`reqnroll.json`. `SutAvailability` is the borderline case — it only probes two configured URLs — and
stays test-side until a second consumer wants the same guard.

## 14. Where else to read

| Question | File |
|---|---|
| Build, pack, smoke test, publishing | [README.md](README.md) |
| The contract as consumers see it | [HardPoint.Testing/README.md](HardPoint.Testing/README.md) |
| What changed in each version | [CHANGELOG.md](CHANGELOG.md) |
| Why the repos are split this way | §13 above |
| Agent-facing rules | [AGENTS.md](AGENTS.md), [CLAUDE.md](CLAUDE.md) |
