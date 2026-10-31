# Split plan: `HardPoint.Testing` NuGet package + demo test project

Plan for splitting the current single solution (`Framework` + `Tests`) into two
deliverables:

1. **A framework project**, published to NuGet, consumed as a package.
2. **A test project**, showing how to use that framework — starting with the
   Playwright + MSTest + Reqnroll flavour in this repository.

> **Status (2026-10-30): complete. All five phases are executed.**
> `HardPoint.Testing` `0.1.0-preview.1` is published on nuget.org (tag `v0.1.0-preview.1`,
> pushed by the Trusted Publishing workflow), and the suite lives in the separate
> **hardpoint-tests** repository, restoring that version from nuget.org with
> `"bindingAssemblies": []` and method-level parallelism on. Verified on its CI: 20/20 scenarios
> pass.
>
> The last two Phase 5 items closed on this date: `smoke.yml` packs the framework, restores the
> `.nupkg` into a real consumer and runs a browser scenario, on every push and as a gate before
> publishing; a weekly scheduled run in the suite repo restores the newest *published* prerelease
> so a framework release that breaks the suite surfaces before anyone bumps the pin.
>
> Everything below is now a record of how the two repositories reached this state. §9 lists what
> the split deliberately left open.

Status legend: `[ ]` not started · `[~]` in progress · `[x]` done · `[!]` blocked

## 1. Goal and non-goals

**Goal.** A versioned `HardPoint.Testing` package whose API is stable enough for
at least one consumer to build against, plus a lean demo repository that
contains only test-side assets (features, steps, page objects, models, config).

**Non-goals for this split.** Not rewriting the driver lifecycle, not adding the
xUnit/NUnit flavours yet, not changing the SUT or its Docker images. Pre-existing
bugs are only fixed where the split would otherwise expose them to package
consumers (see §5).

**Outcome.** One consumer, `hardpoint-tests`, builds against the package and no longer
references framework sources except through the `UseLocalFramework` development switch. The
framework repo contains no suite assets, and the suite repo contains no framework code.

## 2. Current state (before Phase 1)

Baseline captured before any Phase 1 work; see §4 for which items have since been
addressed. `TestAutomation.slnx` contained two projects; `Tests` consumed `Framework`
through a `ProjectReference` (`Tests/Tests.csproj`). `Framework` was 9 files, 692 lines:

| File | Lines | Issue for packaging |
|---|---|---|
| `Framework/Config/ConfigReader.cs` | 253 | Reads `appsettings[.CI].json` from the executing assembly directory; filename not configurable; failure surfaces as `TypeInitializationException` from the static initialiser |
| `Framework/Driver/PlaywrightDriver.cs` | 178 | Writes `traces/` and `sut-*.har` next to the assembly |
| `Framework/Helpers/ExtentReportManager.cs` | 99 | `static ExtentReports` guarded by `new Lock()` per call; writes `TestReports.html` to the current directory |
| `Framework/Driver/BaseHook.cs` | 92 | Hard-codes the mandatory-tag policy; carries `[assembly: Parallelize]` (see §5.2) |
| `Framework/Driver/BasePage.cs` | 20 | Clean |
| `Framework/Config/TestSettings.cs` | 19 | Exposes Playwright option types (`BrowserTypeLaunchOptions`, `APIRequestNewContextOptions`) — config JSON shape is therefore coupled to Playwright |
| `Framework/Constants/ScenarioTags.cs` | 17 | `MandatoryEnvTags` omits `Local`, contradicting `BaseHook` (see §5.3) |
| `Framework/Config/DriverType.cs` | 8 | Clean |
| `Framework/Constants/SettingContstants.cs` | 6 | Filename typo (`SettingContstants`) |

`Framework.csproj` referenced `ExtentReports`, `Microsoft.Playwright` and
`Reqnroll.MSTest`; all three became package dependencies.

Assets that stayed test-side: `Features/`, `Steps/`, `Pages/`, `Models/`,
`Support/ProductsApi.cs`, `Support/ProductNames.cs`, `Support/SutAvailability.cs`,
`Hooks/Hooks.cs`, `appsettings*.json`, `reqnroll.json`, `test.runsettings`,
`docker-compose.yml`.

## 3. Target shape

| Deliverable | Location | Contents |
|---|---|---|
| `HardPoint.Testing` package | This repository (`playwright-dotnet-mstest-reqnroll`) | Framework sources, framework unit tests, the package smoke project, `global.json`, trimmed `Directory.Packages.props`, README, CHANGELOG, publish workflow |
| Demo test project | `hardpoint-tests` repository | `Tests/` only, consuming `HardPoint.Testing` via `PackageReference` pinned by the `HardPointTestingVersion` property in `Directory.Packages.props` (CPM enforced) |

### Decisions

| Decision | Recommendation | Rationale |
|---|---|---|
| Separate repos vs. monorepo | **Separate repositories** | Makes the package boundary real; the demo repo stays free of framework internals. A monorepo (`src/` + `samples/`) is viable if cross-repo PR overhead is unacceptable, at the cost of the package quietly depending on project internals. |
| Runner coupling | **v1: one MSTest + Reqnroll package.** Split into `HardPoint.Testing` (core) + `.MSTest` + `.Reqnroll` when a second runner flavour is actually added | `Reqnroll.MSTest` in the framework drags MSTest into every consumer; splitting now doubles packaging work for no present benefit. |
| Package id | `HardPoint.Testing` (namespace `HardPoint.Testing.*`) | `Framework` is too generic for a public package id and namespace. |
| Initial version | `0.1.0-preview.1`; `1.0.0` once the consumer migration proves the API | Leaves room to break the API during migration. |
| Dev loop | Conditional `ProjectReference` behind `-p:UseLocalFramework=true`, default `false` | CI and fresh clones always consume the published package; framework work stays fast. |
| Feed | nuget.org via Trusted Publishing (`NuGet/login@v1`) | No long-lived API key; keys remain only for local manual pushes. |

## 4. Phases

### Phase 0 — spike the blocking unknown

- [x] Packed the framework, published it to a folder feed, replaced the `ProjectReference` in
      `Tests.csproj` with a `PackageReference`, and ran `@api` and `@ui` scenarios.
- [x] Reqnroll still discovers `BaseHook`'s `[Binding]` from the package assembly: the suite's
      `reqnroll.json` keeps `"bindingAssemblies": []`.
- [x] MSTest still runs scenarios in parallel after removing the `ProjectReference`.

**Exit criteria met:** no consumer-side binding registration is needed, and the package README
records that parallelism is the consumer's to declare.

### Phase 1 — harden the framework in place (still one solution)

- [x] Renamed namespace and assembly: `Framework.*` → `HardPoint.Testing.*`. No `Framework`
      path or namespace survives in either repository.
- [x] `Constants/SettingContstants.cs` is gone; the constant it held moved to its single
      consumer, so the typo disappeared with it.
- [x] Single-sourced the tag vocabulary: `ScenarioTags.MandatoryEnvTags` includes `Local`, and
      `BaseHook.VerifyMandatoryScenarioTags` reads both sets from `ScenarioTags`.
- [x] Tag enforcement is opt-in: `enforceScenarioTags` defaults to `false`; the suite sets it to
      `true`. Covered by `ArtifactDirectoryTests.ScenarioTagEnforcementIsOffWhenTheSettingIsAbsent`.
- [x] Moved `[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]` into the consumer's
      `AssemblyInfo.cs` (suite and smoke project each declare it).
- [x] `ConfigReader`: reads from `AppContext.BaseDirectory`, honours `TEST_CONFIG_FILE`, and
      fails with a `FileNotFoundException` naming the resolved path and the remedy.
- [x] Collapsed artifact paths (traces, HAR, screenshots, `TestReports.html`) behind
      `ArtifactDirectory`, routed by `outputDirectory`.
- [x] Made `ExtentReportManager` thread-safe: one static lock guards reporter and feature-node
      creation, and feature nodes are cached by title.
- [x] Package metadata: `PackageId`, `Version`, `Authors`, `Description`, `PackageTags`,
      `PackageLicenseExpression` (MIT, so no `LICENSE` file is required), `PackageReadmeFile`,
      `RepositoryUrl`, `RepositoryType`, `IncludeSymbols`, `SymbolPackageFormat`.
- [x] Framework unit test project covering `ConfigReader` and `ArtifactDirectory` (14 tests).

**Progress:** 10 of 10 items complete.

### Phase 2 — extract and publish

- [x] Created this repository with the framework sources, framework unit tests, `global.json`,
      `.editorconfig`, `nuget.config` and a trimmed `Directory.Packages.props`.
- [x] Added `README.md` (packed as the package readme), `CHANGELOG.md`, and the licence as an
      expression instead of a file.
- [x] Added a PR workflow: restore, build, unit test, `dotnet format --verify-no-changes`, pack,
      upload.
- [x] Added `publish.yml` on tag `v*` with `permissions: id-token: write` and `NuGet/login@v1`.
- [x] Registered the Trusted Publishing policy (repository, workflow file name, `HardPoint.*`
      scopes); `NUGET_USER` is the only stored secret.
- [x] Tagged `v0.1.0-preview.1` (2026-10-30) and published. The nuget.org flat-container index
      lists exactly that version.

### Phase 3 — migrate the consumer

- [x] Removed `Framework/` from the suite repository and dropped the `ProjectReference`.
- [x] Added the `HardPoint.Testing` pin to `Directory.Packages.props` and a bare
      `<PackageReference>` to `Tests.csproj` (CPM: no `Version` attribute in the csproj).
- [x] Added `Tests/AssemblyInfo.cs` with the `Parallelize` attribute and turned tag enforcement
      on in `appsettings.json`.
- [x] `bindingAssemblies` stays `[]`; Phase 0 showed no entries are needed, and the package
      README says so.
- [x] Aligned pinned versions with the package's dependencies (`Microsoft.Playwright` 1.63.0,
      `Reqnroll.MSTest` 3.3.4, `ExtentReports` 5.0.4), and pinned `System.Drawing.Common` for
      the advisory ExtentReports brings in.
- [x] Updated `README.md`, `AGENTS.md`, `.kiro/steering/*` and `.github/instructions/*` to
      describe the package dependency instead of an in-repo framework project.
- [x] Cleared the NuGet cache, restored, and ran the suite headless against the Docker SUT:
      20/20 scenarios pass.

**Exit criteria met:** green from a clean cache; `TestReports.html`, `traces/`, `screenshots/`
and HAR files land in `artifacts/tests`, which the upload steps consume; scenarios run in
parallel.

### Phase 4 — local development loop

- [x] Added the conditional in `Tests/Tests.csproj` (not `Directory.Build.props` as first
      sketched — one project needs it, and the fail-fast target has to sit beside it):

      ```xml
      <ProjectReference Include="$(LocalFrameworkPath)" Condition="'$(UseLocalFramework)' == 'true'" />
      <PackageReference Include="HardPoint.Testing" Condition="'$(UseLocalFramework)' != 'true'" />
      ```

      A missing `LocalFrameworkPath` fails the build with the resolved path in the message via
      the `EnsureLocalFrameworkExists` target rather than silently falling back to the package.
- [x] Documented `dotnet test -p:UseLocalFramework=true -p:LocalFrameworkPath=<path>`.
- [x] Documented the "true package behaviour" check: plain restore from nuget.org, bump the pin.
- [x] No folder feed to gitignore: `nuget.config` lists nuget.org alone.

### Phase 5 — close the loop

- [x] Framework CI runs a real Playwright scenario against the packed package: `smoke.yml`
      packs `0.0.0-smoke`, asserts the `.nupkg` exists, restores the smoke project from it,
      installs Chromium through the package's own `playwright.ps1`, runs the scenario, and
      asserts that exactly one test ran and that the report contains its title.
- [x] The suite's `dotnet.yml` keeps its push/PR job and adds a weekly scheduled run that
      restores the newest published prerelease (`HardPointTestingVersion` from the environment)
      to catch breakage early.
- [x] Published the versioning policy in the README, `AGENTS.md` and `CHANGELOG.md`: any binding
      signature, config-shape or tag-vocabulary change is at least a minor bump; patches
      otherwise.

## 5. Landmines introduced by the split

### 5.1 Reqnroll binding discovery (highest risk) — ✅ resolved

`BaseHook` is a `[Binding]` class in a package assembly. Reqnroll discovers it from the package
reference with `"bindingAssemblies": []`, verified in the suite and again by the smoke project,
which is the check that would have caught the silence described here: `@ui` scenarios losing
`OpenApplication`, tracing, screenshots and ExtentReports while consumer hooks still run.

### 5.2 `[assembly: Parallelize]` silently stops working — ✅ resolved

The attribute used to sit in the framework's `BaseHook.cs`; once packaged it would configure the
framework assembly, and the consumer's tests would keep passing while running serially. It now
lives in the consumer's `AssemblyInfo.cs`, MSTest's run log confirms method-level parallelism,
and the package README tells consumers it is theirs to declare.

### 5.3 Tag vocabulary had two definitions — ✅ resolved

`Local` is in the public set and `BaseHook` no longer keeps a second copy.

### 5.4 The config-file contract is external API — ✅ resolved

Documented in the package README and enforced by unit tests: file names and location,
`TEST_` overrides limited to declared keys, `TEST_CONFIG_FILE`, and an actionable error naming
the path and the remedy.

### 5.5 Version drift across two repositories — ✅ resolved

The suite pins one property, `HardPointTestingVersion`, overridable from the environment; the
weekly canary surfaces a release that breaks the suite before the pin is bumped. Prereleases may
be bumped from `main`, and the tag check in `publish.yml` refuses a version that does not match
the tag.

### 5.6 Repository hygiene — ✅ resolved

`artifacts/`, `TestReports.html`, `allure-results/` and `*.feature.cs` are ignored. `Tests/history.jsonl`
was tracked during the Allure experiment; Allure has since been removed from the suite along with
the history file, `allureConfig.json` and `allurerc.json`, leaving ExtentReports as the single
report.

### 5.7 Pre-existing framework bugs made public — ✅ resolved (driver items documented)

Fixed: the `ExtentReportManager` static lock and the duplicate feature nodes per scenario.
Deferred and recorded in `CHANGELOG.md` under *Known limitations*:
`PlaywrightDriver.Dispose` blocking on `Task.Run(...).Wait()`, `Page => _page.Value.Result`, and
`BaseHook` screenshotting every `@ui` scenario because the failure-only guard is commented out.

## 6. Risks

| Risk | Impact | Mitigation |
|---|---|---|
| Reqnroll bindings not discovered from the package | Silent loss of UI hooks, tracing, reporting | Resolved by Phase 0; the smoke project fails if the scenario is not generated |
| `Parallelize` attribute lost | Silent 5–10× slower suites | Moved to the consumer assembly; parallelism verified in the run log |
| Config contract misunderstood by consumers | `TypeInitializationException` on first run | README section plus unit tests on the failure messages |
| Two-repo overhead | Slow iteration | `UseLocalFramework` switch; prereleases publish freely |
| Package dependency footprint (Playwright, ExtentReports, Reqnroll, MSTest) | Version conflicts in consumers | Supported versions documented; the weekly canary catches drift |

## 7. Explicitly not in the package

`SutAvailability`, `ProductsApi`, `ProductNames`, page objects, step definitions, feature files,
`docker-compose.yml`, `appsettings*.json`, `reqnroll.json`, `test.runsettings`.

`SutAvailability` is still the borderline case — it merely probes two configured URLs — and stays
test-side until a second consumer wants the same guard.

## 8. Acceptance checklist

- [x] Package installs into a consumer and the readme renders on nuget.org
      (`0.1.0-preview.1` is the only published version and the smoke project restores it).
- [x] Demo repository restores, builds and runs the full suite from a cleared NuGet cache
      against the Docker SUT.
- [x] `TestReports.html`, `traces/`, `screenshots/` and `*.har` appear for the CI upload steps.
- [x] Scenarios demonstrably run in parallel in the demo repository.
- [x] A missing `appsettings.json` produces an actionable error naming the path.
- [x] `dotnet pack` produces `.nupkg` + `.snupkg`.
- [x] Publishing works through Trusted Publishing only — no long-lived API key in either
      repository.

## 9. What the split leaves open

Split mechanics are done; what remains is framework hygiene and suite improvements, not
migration work. The framework items raised by the split are now closed:

- ✅ `BaseHook.AttachScreenshotOnFailure` captures only failed `@ui` scenarios again.
- ✅ `PlaywrightDriver` no longer blocks on `Task.Run(...).Wait()` or guesses with fixed 500 ms
  delays: `DisposeAsync` closes page, context, browser, API context and the driver in order, and
  `Page` unwraps launch failures instead of throwing `AggregateException`.
- ✅ The suite's `test.runsettings` — commented-out boilerplate that CI never applied — is
  deleted, along with the property and copy item that referenced it.

Still open, deliberately:

- `PlaywrightDriver.Page` remains a synchronous bridge over the lazy page, because page objects
  expose synchronous locators. Async code uses `GetPageAsync()`; a fully async page API would be
  a breaking change for consumers.
- The suite's backlog: flakiness detection first, then contract, data-driven and accessibility
  coverage — see `hardpoint-tests/SUGGESTIONS.md`.
