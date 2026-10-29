# Split plan: `HardPoint.Testing` NuGet package + demo test project

Plan for splitting the current single solution (`Framework` + `Tests`) into two
deliverables:

1. **A framework project**, published to NuGet, consumed as a package.
2. **A test project**, showing how to use that framework — starting with the
   Playwright + MSTest + Reqnroll flavour in this repository.

> **Status (2026-10-29):** Phases 0–3 are executed. The framework builds as the
> `HardPoint.Testing` package (`HardPoint.Testing/`, namespaces `HardPoint.Testing.*`,
> `0.1.0-preview.1`), and the suite lives in the separate **hardpoint-tests** repository,
> consuming that version from a local folder feed. Verified there: 20/20 scenarios pass and
> Reqnroll discovers `BaseHook` from the package with `"bindingAssemblies": []`. Outstanding:
> Phase 4 (local `UseLocalFramework` switch) and Phase 5 (framework CI running a real scenario,
> publishing to nuget.org). The checkboxes below record the state when each phase was worked on.

Status legend: `[ ]` not started · `[~]` in progress · `[x]` done · `[!]` blocked

## 1. Goal and non-goals

**Goal.** A versioned `HardPoint.Testing` package whose API is stable enough for
at least one consumer to build against, plus a lean demo repository that
contains only test-side assets (features, steps, page objects, models, config).

**Non-goals for this split.** Not rewriting the driver lifecycle, not adding the
xUnit/NUnit flavours yet, not changing the SUT or its Docker images. Pre-existing
bugs are only fixed where the split would otherwise expose them to package
consumers (see §5).

## 2. Current state (before Phase 1)

Baseline captured before any Phase 1 work; see §4 for which items have since been
addressed. `TestAutomation.slnx` contains two projects; `Tests` consumes `Framework`
through a `ProjectReference` (`Tests/Tests.csproj`). `Framework` is 9 files, 692 lines:

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
| `Framework/Constants/SettingContstants.cs` | 6 | Filename typo (`SettingContstants`) — rename while it is still internal churn |

`Framework.csproj` references `ExtentReports`, `Microsoft.Playwright` and
`Reqnroll.MSTest`; all three become package dependencies. `dotnet pack` already
succeeds today but produces `Framework.1.0.0.nupkg` with no metadata and warns
about a missing readme.

Assets that must stay test-side: `Features/`, `Steps/`, `Pages/`, `Models/`,
`Support/ProductsApi.cs`, `Support/ProductNames.cs`, `Support/SutAvailability.cs`,
`Hooks/Hooks.cs`, `appsettings*.json`, `reqnroll.json`, `test.runsettings`,
`allureConfig.json`, `allurerc.json`, `docker-compose.yml`.

## 3. Target shape

| Deliverable | Location | Contents |
|---|---|---|
| `HardPoint.Testing` package | New repository `hardpoint-test-framework` | Framework sources + framework unit tests + `global.json` + trimmed `Directory.Packages.props` + README/CHANGELOG/LICENSE + publish workflow |
| Demo test project | This repository | `Tests/` only, consuming `HardPoint.Testing` via `PackageReference` pinned in `Directory.Packages.props` (CPM is already enforced) |

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

- [ ] Pack the current `Framework`, publish it to a local folder feed, replace the
      `ProjectReference` in `Tests.csproj` with a `PackageReference`, and run one
      `@api` and one `@ui` scenario.
- [ ] Record whether Reqnroll still discovers `BaseHook`'s `[Binding]` from the
      package assembly. `Tests/reqnroll.json` currently has `"bindingAssemblies": []`.
- [ ] Record whether MSTest still runs scenarios in parallel after removing the
      `ProjectReference` (see §5.2).

**Exit criteria:** a written answer to both questions. If bindings are *not*
discovered, the fix (listing the assembly in `bindingAssemblies`, or explicit
binding registration) must be part of the framework README before Phase 2, because
it changes what every consumer has to write.

### Phase 1 — harden the framework in place (still one solution)

Refactoring is cheaper while both projects are in one solution.

- [ ] Rename namespace and assembly: `Framework.*` → `HardPoint.Testing.*`,
      `Framework.csproj` → `HardPoint.Testing.csproj`. Touches all framework files
      plus `Tests/Hooks/Hooks.cs` and every `using Framework.*`.
- [ ] Rename `Constants/SettingContstants.cs` → `SettingConstants.cs` (the class inside
      is already named `SettingConstants`; only the file name is misspelled).
- [x] Single-source the tag vocabulary: `ScenarioTags.MandatoryEnvTags` now includes
      `Local`, and `BaseHook.VerifyMandatoryScenarioTags` reads both sets from
      `ScenarioTags` instead of building private copies. Verified: 20/20 scenarios tagged
      `@local` pass the guard.
- [ ] Make tag enforcement opt-in via settings; keep it on for this demo project.
- [x] Move `[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]` out of
      `Framework/Driver/BaseHook.cs` into `Tests/AssemblyInfo.cs`. Verified: MSTest logs
      `Parallelization enabled for ...Tests.dll (Workers: 8, Scope: MethodLevel)` and TRX
      timings show 8 scenarios running concurrently (176 s wall clock vs 1380 s of
      cumulative test time).
- [x] `ConfigReader`: reads from `AppContext.BaseDirectory`, honours `TEST_CONFIG_FILE`
      (relative values resolved against the test output directory), and fails with a
      `FileNotFoundException` naming the resolved path and the remedy instead of a
      `TypeInitializationException` hidden behind the static initialiser. Verified for a
      missing override, a missing default file, and a custom-named config file.
- [ ] Collapse artifact paths (traces, HAR, screenshots, `TestReports.html`) behind
      one configurable output directory, defaulting to the current CI-expected
      locations under the test output folder.
- [x] Make `ExtentReportManager` thread-safe: one static `Lock` now guards reporter and
      feature-node creation (previously `lock (new Lock())` excluded nothing), and feature
      nodes are cached by title. Verified: the report contains one block per feature
      (2) for 20 scenarios, previously one per scenario.
- [ ] Add package metadata to the csproj: `PackageId`, `Version`, `Authors`,
      `Description`, `PackageTags`, `PackageLicenseExpression`, `PackageReadmeFile`
      (+ `None ... Pack="true"` item), `RepositoryUrl`, `RepositoryType`,
      `IncludeSymbols`, `SymbolPackageFormat=snupkg`.
- [x] Add a framework unit test project (`Framework.Tests`, to be renamed with the
      assembly in this phase) covering `ConfigReader`: bare leaf names, `__`-separated
      paths, collections, unknown keys, missing file, missing override, relative vs.
      absolute `TEST_CONFIG_FILE` resolution and malformed JSON. 9 tests. Enum overrides
      are not covered because `TestSettings` has no enum property yet.

**Progress:** 5 of 10 items complete. All four were verified by a full suite run
against the Docker SUT (20/20 passing) as well as `dotnet build`, `dotnet format
--verify-no-changes` and `-p:AnalysisLevel=latest-recommended` (all clean).

**Exit criteria:** `dotnet build` and `dotnet test` green in the current repo;
`dotnet pack` yields a package with readme, symbols and the expected three
dependencies; Phase 0 spike passes end-to-end against the new package.

### Phase 2 — extract and publish

- [ ] Create the framework repository, move framework sources + unit tests, copy
      `global.json`, `.editorconfig`, `nuget.config`, and a trimmed
      `Directory.Packages.props`.
- [ ] Add `README.md` (install, config-file contract, `bindingAssemblies` note,
      Playwright browser install step), `CHANGELOG.md`, `LICENSE`.
- [ ] Add a PR workflow: restore, build, unit test, `dotnet pack`.
- [ ] Add `publish.yml` on tag `v*`: `permissions: id-token: write`,
      `NuGet/login@v1` with `user: ${{ secrets.NUGET_USER }}`, then
      `dotnet nuget push artifacts/*.nupkg --api-key ${{ steps.login.outputs.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json --skip-duplicate`.
- [ ] Register the Trusted Publishing policy (repository owner, repository,
      workflow **file name only**, scopes globbed to `HardPoint.*`); add
      `NUGET_USER` as the only stored secret.
- [ ] Tag `v0.1.0-preview.1`, publish, and verify the nuget.org page shows the
      readme and the dependency list.

**Exit criteria:** the package appears on nuget.org and installs into a scratch
project.

### Phase 3 — migrate the consumer

- [ ] Remove `Framework/` from this repo and drop the `ProjectReference` from
      `Tests/Tests.csproj`.
- [ ] Add `<PackageVersion Include="HardPoint.Testing" Version="0.1.0-preview.1" />`
      to `Directory.Packages.props` and a bare `<PackageReference Include="HardPoint.Testing" />`
      to `Tests.csproj` (CPM: never put a `Version` attribute in the csproj).
- [ ] Add `Tests/AssemblyInfo.cs` with the `Parallelize` attribute and turn tag
      enforcement on in `appsettings.json`.
- [ ] Add `reqnroll.json` `bindingAssemblies` entries if Phase 0 showed they are needed.
- [ ] Align pinned versions in `Directory.Packages.props` with the package's
      dependencies (`Microsoft.Playwright`, `Reqnroll.MSTest`, `ExtentReports`).
      With `CentralPackageTransitivePinningEnabled=true`, drift shows up as
      NU1605/NU1608.
- [ ] Update `README.md`, `AGENTS.md`, `.kiro/steering/*` and
      `.github/instructions/tests.instructions.md` to describe the package
      dependency instead of the in-repo framework project.
- [ ] `dotnet nuget locals http-cache --clear`, then restore and run the suite
      headless against the Docker SUT.

**Exit criteria:** the full suite is green from a clean cache; `TestReports.html`,
`traces/`, `screenshots/` and HAR files still land in `artifacts/tests` so the
existing `dotnet.yml` upload steps keep working; scenarios still run in parallel.

### Phase 4 — local development loop

- [ ] Add a conditional to `Directory.Build.props` (in this repo):

      ```xml
      <ItemGroup Condition="'$(UseLocalFramework)' == 'true' AND Exists('$(LocalFrameworkPath)')">
        <ProjectReference Include="$(LocalFrameworkPath)" />
      </ItemGroup>
      <ItemGroup Condition="'$(UseLocalFramework)' != 'true'">
        <PackageReference Include="HardPoint.Testing" />
      </ItemGroup>
      ```

- [ ] Document: `dotnet test -p:UseLocalFramework=true -p:LocalFrameworkPath=../hardpoint-test-framework/src/HardPoint.Testing/HardPoint.Testing.csproj`.
- [ ] Document the "true package behaviour" check: `dotnet pack -o ../localfeed`,
      add `localfeed` as a `nuget.config` source, bump the pinned version.
- [ ] Note that `localfeed/` must be gitignored.

### Phase 5 — close the loop

- [ ] Framework CI runs at least one real Playwright scenario (browsers installed),
      so "packs fine, breaks consumers" cannot ship silently.
- [ ] This repo's `dotnet.yml` unchanged; add a scheduled job that restores the
      latest prerelease to catch breakage early.
- [ ] Publish a versioning policy: any binding signature, config-shape or tag
      vocabulary change is at least a minor bump; patches otherwise.

## 5. Landmines introduced by the split

### 5.1 Reqnroll binding discovery (highest risk)

`BaseHook` is a `[Binding]` class in `Framework.dll`. Today it is discovered
because `Framework` is a `ProjectReference`. If Reqnroll's generator does not scan
NuGet-referenced assemblies, `@ui` scenarios silently lose `OpenApplication`,
tracing, failure screenshots and ExtentReports, while consumer-side hooks such as
`DeleteCreatedProduct` still run — a partial and confusing failure. Phase 0 exists
to settle this before any code moves.

### 5.2 `[assembly: Parallelize]` silently stops working — ✅ resolved

The attribute used to sit at the top of `Framework/Driver/BaseHook.cs`. Once packaged
it would apply to the *framework* assembly, not the consumer's test assembly: tests
keep passing but run sequentially — an invisible slowdown, not an error.

Resolved in Phase 1: the attribute now lives in `Tests/AssemblyInfo.cs`, and MSTest's
run log plus TRX timings confirm method-level parallelism (8 workers, 8 concurrent
scenarios). Any consumer of the package must declare it in its own test assembly; this
belongs in the package README.

### 5.3 Tag vocabulary has two definitions today — ✅ resolved

`ScenarioTags.MandatoryEnvTags` excluded `Local`, while `BaseHook` built its own set
that included it, so a `@local` scenario passed validation with `BaseHook` but would
be rejected by any consumer using the public set.

Resolved in Phase 1: `Local` is in the public set and `BaseHook` no longer keeps a
second copy.

### 5.4 The config-file contract becomes external API — ⚠️ partly addressed

Consumers must name files `appsettings.json` / `appsettings.CI.json`, place them in
the test output directory, and honour `TEST_` environment overrides. None of this
is discoverable from IntelliSense, so it belongs in the package README.

Phase 1 made the contract friendlier — `AppContext.BaseDirectory` resolution, the
`TEST_CONFIG_FILE` override and an actionable not-found error — but it still has to be
documented in the package README, and it needs the unit tests listed in Phase 1.

### 5.5 Version drift across two repositories

A framework fix needs a version bump before the demo can use it. Decide up front:
prerelease bumps allowed from `main`, stable versions only from tags.

### 5.6 Repository hygiene

Verified before writing this plan: `artifacts/`, `TestReports.html`, `allure-report/`,
`allure-results/` and `*.feature.cs` are already ignored. Two exceptions:

- `Tests/history.jsonl` **is tracked** — Allure history, not source. Decide whether
  to untrack it (it changes on every run, so it creates noise in every diff).
- Untracked build leftovers exist on disk from a previous SUT build:
  `ProductAPI/bin`, `ProductAPI/obj`, `WebApp/bin`, `WebApp/obj`, and two
  `.csproj.user` files. They are not tracked; delete them rather than moving the
  folders into the new repo.

### 5.7 Pre-existing framework bugs made public — ✅ resolved (driver items deferred)

Reachable from a package consumer and fixed in Phase 1: the `ExtentReportManager`
static lock (`new Lock()` per call) and duplicate feature nodes per scenario.

Still deferred, to be listed in the CHANGELOG: `PlaywrightDriver.Dispose` blocking on
`Task.Run(...).Wait()` and `Page => _page.Value.Result`.

## 6. Risks

| Risk | Impact | Mitigation |
|---|---|---|
| Reqnroll bindings not discovered from the package | Silent loss of UI hooks, tracing, reporting | Phase 0 spike; `bindingAssemblies` fix documented in the package README |
| `Parallelize` attribute lost | Silent 5–10× slower suites | Phase 1 move + Phase 3 parallel-run verification |
| Config contract misunderstood by consumers | `TypeInitializationException` on first run | Configurable filename, actionable error message, README section, framework unit tests |
| Namespace rename churn | Large diff | Do it in Phase 1 while both projects are in one solution |
| Two-repo overhead | Slow iteration | `UseLocalFramework` switch; publish prereleases freely |
| Package dependency footprint (Playwright, ExtentReports, Reqnroll, MSTest) | Version conflicts in consumers | Document supported versions; keep `Directory.Packages.props` entries aligned |

## 7. Explicitly not in the package

`SutAvailability`, `ProductsApi`, `ProductNames`, page objects, step definitions,
feature files, `docker-compose.yml`, `appsettings*.json`, `reqnroll.json`,
`test.runsettings`, Allure configuration.

`SutAvailability` is the only borderline case — it merely probes two configured
URLs — but its health-check contract is not settled; keep it test-side for v1 and
promote it later if a second consumer wants the same guard.

## 8. Acceptance checklist

- [ ] Package installs into a clean scratch project and the readme/licence render
      on nuget.org.
- [ ] Demo repository restores, builds and runs the full suite from a cleared
      NuGet cache against the Docker SUT.
- [ ] `TestReports.html`, `traces/`, `screenshots/` and `*.har` still appear in
      `artifacts/tests` for the CI upload steps.
- [ ] Scenarios demonstrably run in parallel in the demo repository.
- [ ] A missing `appsettings.json` produces an actionable error naming the path.
- [ ] `dotnet pack` in the framework repo produces `.nupkg` + `.snupkg` with no
      warnings.
- [ ] Publishing works through Trusted Publishing only — no long-lived API key
      stored in either repository.
