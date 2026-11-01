# HardPoint.Testing — improvement suggestions

Ideas for improving the framework package, gathered from a review of its sources, its tests, its
packaging and the two workflows that ship it. Each item names the evidence that prompted it.

**Method.** Code review of `HardPoint.Testing/` (8 sources, a public surface of roughly fifty members), `HardPoint.Testing.Tests/`
(14 tests) and `HardPoint.Testing.SmokeTests/`; then a comparison against how the ecosystem handles the
same problems — the [Reqnroll parallel-execution guidance](https://docs.reqnroll.net/latest/execution/parallel-execution.html),
[Playwright's authentication guidance](https://playwright.dev/dotnet/docs/auth) and
[.NET package validation](https://learn.microsoft.com/en-us/dotnet/fundamentals/apicompat/package-validation/overview),
plus public Reqnroll+Playwright frameworks such as
[Romfos/AutoTests.Framework](https://github.com/Romfos/AutoTests.Framework) and
[rohitsaraswat7865/Reqnroll_Playwright_CSharp](https://github.com/rohitsaraswat7865/Reqnroll_Playwright_CSharp).

**Ordering.** Highest risk first. Items marked **[contract]** change or clarify the consumer-facing
surface and therefore need at least a minor version bump.

---

## 🔴 Correctness & contract

### 1. The HAR filter is hard-coded to this demo's URLs — a leak from the SUT into the package — ✅ done
`PlaywrightDriver` records a HAR for every browser context, filtered to `**/Product/**`:

```csharp
return await (await _browser.Value).NewContextAsync(new BrowserNewContextOptions
{
    RecordHarPath = harPath,
    RecordHarUrlFilterString = "**/Product/**"     // PlaywrightDriver.cs:128-129
});
```

`Product` is HardPoint's domain, not the package's. A consumer testing anything else silently gets a
HAR containing almost nothing — precisely the artifact they would reach for when a CI run fails.

**Suggestion:** move the filter into configuration (`harOptions.urlFilter`, default `**/*`) or drop it
and record everything, then honour `recordHar: false` to turn recording off. This is the clearest
example of SUT-specific knowledge that survived the split; check for others while fixing it.

**Done in 0.1.0-preview.3.** `harOptions.urlFilter` now defaults to `**/*` and `harOptions.recordHar`
(default `true`) turns recording off, which also settles item 8. The unit tests pin the options the
driver builds, and a browser check confirmed the old filter wrote zero entries for a non-Product
site while `**/*` writes them all. The remaining sources were re-checked for SUT-specific values:
none — the only URLs left are the ones the consumer configures (`applicationUrl`,
`apiOptions.BaseURL`).

### 2. `browserType` is validated only when the browser launches — ✅ done
The switch validates the setting at the point of first use:

```csharp
IBrowserType browserType = _testSettings.BrowserType switch
{
    BrowserType.Chromium => ...,
    _ => throw new ArgumentException($"Unsupported browser type: {_testSettings.BrowserType}"),
};
```

A typo (`"chrome"` instead of `"chromium"`) therefore fails **mid-scenario**, after `BeforeScenario`
hooks have opened a report node, with a message that does not name the file, the key or the accepted
values.

**Suggestion:** validate in `ConfigReader.ReadConfig` right after deserialisation and throw the same
actionable shape the missing-file case already uses — resolved path, key, accepted values. The
documented contract says `chromium`, `firefox` or `webkit`; enforce it where it is read.

**Done in 0.1.0-preview.4.** `ReadConfig` validates after deserialisation, so `ConfigReader.Settings`
itself throws with the resolved path and `'browserType' must be one of 'chromium', 'firefox',
'webkit', but was 'chrome'`. Matching is case-insensitive and the canonical spelling is stored, so
the driver's switch only ever sees a valid value. Tests cover all three documented engines, the
mixed-case spelling and the rejected value, all without launching a browser.

### 3. The parallelism obligation fails silently — it can be detected instead — ✅ done
The package's biggest trap is documented in the README: `[assembly: Parallelize]` must live in the
consumer's assembly, and if it does not, the suite passes while running serially ([Reqnroll's docs
say the same](https://docs.reqnroll.net/latest/execution/parallel-execution.html): MSTest runs
nothing in parallel by default). Documentation only helps the people who read it.

**Suggestion:** detect it once per run and warn, since the package can see the consumer's assembly
from a concrete hook type:

```csharp
[BeforeScenario(Order = int.MinValue + 1)]
public void WarnWhenParallelismIsNotConfigured(IReqnrollOutputHelper output)
{
    if (_warned) return;
    _warned = true;

    if (GetType().Assembly.GetCustomAttribute<ParallelizeAttribute>() is null)
    {
        output.WriteLine("HardPoint.Testing: this assembly declares no [assembly: Parallelize] " +
                         "attribute, so scenarios run serially. See the package README.");
    }
}
```

A warning rather than an exception keeps deliberately serial consumers working; a
`warnOnSerialExecution` setting can turn it off. While in this area, consider surfacing Reqnroll's
`addNonParallelizableMarkerForTags` — the supported way to exclude tag-marked features from parallel
runs.

**Done in 0.1.0-preview.5.** `BaseHook.WarnWhenParallelismIsNotConfigured` runs at
`Order = int.MinValue + 1`, warns once per test process through `IReqnrollOutputHelper`, and names
the assembly, the missing attribute and the `warnOnSerialExecution` opt-out. The attribute is matched
from metadata by name instead of through `GetCustomAttribute<ParallelizeAttribute>()`: the package has
no compile-time MSTest reference, and taking one bound the check to MSTest 2.2.8's assembly identity,
which fails to load next to MSTest 4.x. Unit tests cover the warning, the once-per-run guard, the
opt-out and both directions of the attribute check; the packaged README points at Reqnroll's
`addNonParallelizableMarkerForTags` for tag-scoped exclusions.

### 4. There is no guard against accidental breaking changes **[contract]** — ✅ done
A public surface of roughly fifty members, a semver policy in the README and CHANGELOG ("a binding signature, config-shape or
tag-vocabulary change is at least a minor bump") — all enforced by review discipline alone. A
published version can be unlisted but never deleted, so a break that ships stays shipped.

**Suggestion:** enable .NET package validation and keep a baseline of the last release:

```xml
<EnablePackageValidation>true</EnablePackageValidation>
<PackageValidationBaselineVersion>0.1.0-preview.2</PackageValidationBaselineVersion>
```

The build then fails on an incompatible public API change, in the same run that packs
([docs](https://learn.microsoft.com/en-us/dotnet/fundamentals/apicompat/package-validation/overview)).
Adding `Microsoft.CodeAnalysis.PublicApiAnalyzers` with a committed `PublicAPI.Shipped.txt` makes the
public surface visible in review as well.

**Done in 0.1.0-preview.7.** Package validation is on with the baseline at `0.1.0-preview.2`. Checked
by packing against `0.1.0-preview.1`, where it failed with CP0001/CP0002 for every member preview.2
had removed — the gate is real, not decorative. `PublicApiAnalyzers` was not added: package
validation already fails the build on a break, and the analyzer only adds review visibility; revisit
if the surface grows beyond what a reviewer can hold in their head. Bump
`PackageValidationBaselineVersion` whenever a version is published.

### 5. The symbol package has no source link — ✅ done
`IncludeSymbols`/`SymbolPackageFormat` produce a `.snupkg`, but `PublishRepositoryUrl` and SourceLink
are unset, so a consumer stepping into the package gets no source navigation — the symbol package is
mostly inert.

**Suggestion:** `PublishRepositoryUrl`, `EmbedUntrackedSources` and
`Microsoft.SourceLink.GitHub` (PrivateAssets=all). Cheap, and it makes the `RepositoryUrl` already in
the csproj actually useful.

**Done in 0.1.0-preview.7.** All three properties/package are in place; the packed nuspec now carries
the source commit and branch, and the `.snupkg` PDB gets the source-link map.

---

## 🟡 Capability gaps

### 6. Consumers cannot influence the browser context **[contract]** — ✅ done
`BrowserNewContextOptions` is constructed internally with the HAR path and nothing else, so there is
no supported way to set `StorageStatePath`, `ExtraHTTPHeaders`, `ViewportSize`, `Locale`,
`TimezoneId`, `Permissions`, `BaseURL` or `IgnoreHTTPSErrors`.

[Playwright's own guidance](https://playwright.dev/dotnet/docs/auth) makes storage state the standard
answer to "authenticate once, not per test" — the single most common thing a UI suite needs next, and
today it is unreachable without reflection or a parallel context.

**Suggestion:** add a `browserContextOptions` section to `TestSettings`, deserialised into
`BrowserNewContextOptions` and merged with the framework's own HAR settings (framework wins on the
HAR path, so artifact routing stays consistent). Document it as pass-through, the way
`browserOptions` and `apiOptions` already work.

**Done in 0.1.0-preview.6.** `TestSettings.BrowserContextOptions` is deserialised from
`browserContextOptions` and passed to `NewContextAsync`. `PlaywrightDriver` copies it per context
(`BrowserNewContextOptions` has a copy constructor) before writing the per-context HAR path, so
parallel scenarios cannot race on one shared instance. The framework still owns the archive: the
path always routes to the artifact directory and the filter comes from `harOptions`; with
`harOptions.recordHar: false` the consumer's own HAR configuration is left untouched. Tests cover
pass-through (storage state, headers, viewport, locale, time zone, permissions, base URL, HTTPS
errors), the HAR precedence, the record-off escape hatch, the per-context copy, and config
deserialisation including the `TEST_BrowserContextOptions__Locale` override.

### 7. Every `@ui` scenario is traced, then the trace is thrown away
`OpenApplication` always calls `StartTracingAsync`; `StopTracingAsync(keep: false)` deletes the file
when the scenario passes. Correct for debugging, but the recording cost is paid on every green run,
which is most of them.

**Suggestion:** a `trace` setting with Playwright's familiar semantics — `off`, `on`,
`retain-on-failure` (default, current behaviour). Local runs get faster; CI keeps the artifact.

### 8. A HAR is recorded for every context whether or not anyone wants it — ✅ done
Same shape as item 7: unconditional recording costs disk and I/O, and the artifact only matters for
failures.

**Suggestion:** `recordHar` (default `true` to preserve behaviour) plus the filter from item 1.

**Done with item 1:** `harOptions.recordHar` and `harOptions.urlFilter` ship in 0.1.0-preview.3.

### 9. `BasePage` stops just short of what page objects actually repeat
The helpers (`LinkAsync`, `ButtonAsync`, `RowAsync`, `TextAsync`, `ClickLinkAsync`,
`ClickButtonAsync`) cover clicks and locators, but every form interaction falls back to
`(await GetPageAsync()).GetByLabel(...)`. That pattern is repeated in every method of both page
objects in the consuming suite.

**Suggestion:** add the few helpers that remove the boilerplate without turning `BasePage` into a
framework of its own — `FillAsync(label, value)`, `SelectAsync(label, value)`,
`WaitForLoadStateAsync()`, and a scoped `RowAsync(name).GetByRole(...)` convenience. Keep the
principle that consumers can always drop to raw Playwright.

### 10. No conveniences for API scenarios
`ApiRequestContext` is exposed raw, so consumers re-implement JSON headers and status handling; the
consuming suite's `ProductsApi` repeats the same `Content-Type` dictionary in four methods.

**Suggestion:** a small `ApiClient` helper in the package — `PostJsonAsync`, `PutJsonAsync`,
`GetJsonAsync<T>` — that sets JSON headers and, on a non-success status, attaches the request and
response bodies to the report. That last part is what makes API failures debuggable from
`TestReports.html` instead of from a stack trace.

### 11. Failure artifacts never reach the report
`AttachScreenshotOnFailure` attaches the screenshot, and `SaveTraceOnFailure` attaches the trace path
to the Reqnroll output — but `TestReports.html`, the artifact CI uploads by default, shows neither the
trace nor the HAR name. Anyone reading the report has to know to look in `traces/`.

**Suggestion:** attach the trace path (and HAR file name) to the failing scenario node, as a link or
a labelled entry. The report then points at every artifact for that scenario.

---

## 🟢 Verification & CI

### 12. Two of the three advertised browsers are never exercised
`browserType` documents `chromium`, `firefox` and `webkit`; both workflows install and run
**chromium only**. A release can break two thirds of the advertised surface and stay green.

**Suggestion:** a nightly matrix job (not on every PR, to keep the feedback loop short) running the
smoke scenario against all three engines — the smoke project already exists for exactly this kind of
"does the package work at all" check.

### 13. No coverage, and the interesting half is untested
The 14 unit tests cover `ConfigReader` and `ArtifactDirectory`. `BaseHook` (tag guard, hook ordering,
screenshot-on-failure), `PlaywrightDriver` (lazy resources, ordered teardown, trace retention) and
`ExtentReportManager` (locking, feature-node caching) have no direct tests; the smoke project proves
one happy path end to end and would not notice, say, a screenshot attached to *every* scenario again.

**Suggestion:** add coverage collection (`--collect:"XPlat Code Coverage"`) with a floor, then close
the gaps in that order: driver teardown, screenshot-on-failure guard, report node caching, tag-guard
messages.

### 14. No dependency or security scanning
The `System.Drawing.Common` advisory (GHSA-rxg9-xrhp-64gj) was found by hand and pinned by hand. That
will happen again: the package sits on a chain of transitive dependencies it does not control.

**Suggestion:** Dependabot for `nuget` and `github-actions`, plus a
`dotnet list package --vulnerable --include-transitive` step in `dotnet.yml`.

---

## ⚪ Packaging & docs

### 15. No XML documentation on a public package — ✅ done
`GenerateDocumentationFile` is off, so the public API ships without IntelliSense or nuget.org
API docs — for a package whose whole purpose is to be consumed.

**Suggestion:** enable it, document the public surface as it is touched, and let the compiler flag
undocumented members in new code.

**Done in 0.1.0-preview.7.** `GenerateDocumentationFile` is on, every public and protected member is
documented, `HardPoint.Testing.xml` is inside the `.nupkg`, and CS1591 is promoted to an error so an
undocumented new member fails the build rather than becoming a warning nobody reads.

### 16. No compatibility matrix
A consumer has to read `Directory.Packages.props` to discover that this version pairs with
Playwright 1.63.0, Reqnroll 3.3.4, MSTest 4.5.1 on `net10.0` — and that only **MSTest** is supported,
not the NUnit/xUnit/TUnit flavours Reqnroll also offers.

**Suggestion:** a table in the package README (version → dependency versions → runner), refreshed at
release. It is the first thing a consumer checks and the last thing anyone remembers to update.

### 17. The only usage example is private
`HardPoint.Testing.SmokeTests` is an excellent example — it consumes the packed package, declares its
own `Parallelize`, derives from `BaseHook` and runs a scenario — but it lives outside the solution and
is never shown in the README. A newcomer's reference is the separate `hardpoint-tests` repository.

**Suggestion:** a `samples/` quickstart (or a README walkthrough quoting the smoke project) showing the
minimum: package reference, `appsettings.json`, one hooks class, one page object, one scenario.

---

## Considered and rejected

- **Retry/flakiness support.** MSTest already offers retry attributes; a retry policy belongs to the
  consumer (the suite decided to skip it deliberately). The package only needs to not make it harder.
- **Visual regression, accessibility, test-data builders.** Separate concerns with their own tools;
  the suite added axe-core at its own level, which is the right place.
- **Splitting into `.MSTest` / `.Reqnroll` / core packages.** Still not worth it while there is one
  runner; revisit only if a second flavour is genuinely added (the split plan made the same call).

## Suggested order of attack

1. **Items 7, 11** — make traces deliberate and reachable from the report.
2. **Items 12, 13, 14** — verification: browser matrix, coverage floor, dependency scanning.
3. **Items 9, 10, 16, 17** — ergonomics and docs, as consumers ask for them.
