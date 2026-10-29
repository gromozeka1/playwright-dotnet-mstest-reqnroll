# Repository Guidelines & Agent Instructions

## Solution Architecture & Module Organization

This repository contains a .NET 10 solution (`TestAutomation.slnx`) pinned by `global.json` (`10.0.100`), consisting of two projects. The system under test (SUT) is **not** part of this repository: it runs from published Docker Hub images (`gromozeka1/hardpoint-webapp`, `gromozeka1/hardpoint-productapi`), whose source lives in the separate [gromozeka1/sut-hardpoint](https://github.com/gromozeka1/sut-hardpoint) repository.

| Project | Type | Description |
|---|---|---|
| `Framework/` | Class Library | Shared Playwright test infrastructure: driver lifecycle, config reader, `BasePage`, `BaseHook`, `ExtentReportManager`, and scenario tag constants. |
| `Tests/` | MSTest / Reqnroll | BDD automated test suite: Gherkin feature files (`Features/`), step definitions (`Steps/`), page objects (`Pages/`), hooks (`Hooks/`), API helpers (`Support/`), and test models (`Models/`). |

### Dependency Flow & Isolation
- `Tests` references `Framework`.
- `Tests` has **no compile-time reference** to the SUT; it talks to the services solely over HTTP (Playwright API request context and the browser).
- `Tests` mirrors API models in `Tests/Models/` (POCOs) rather than sharing assemblies.
- Local orchestration is defined in `docker-compose.yml`, which pulls the SUT images from Docker Hub (version pinned via `SUT_TAG`, default `1.0.0`).

---

## Build, Test, and Development Commands

Always run commands from the repository root using the .NET 10 SDK:

```powershell
# Build entire solution
dotnet build TestAutomation.slnx

# Install browser binaries (run after build, once per machine/update; chromium is the default)
pwsh Tests/bin/Debug/net10.0/playwright.ps1 install chromium

# Start the SUT services via Docker Compose
docker compose up -d

# Run MSTest / Reqnroll test suite (SUT must be running)
dotnet test Tests/Tests.csproj

# Run tests headlessly via environment variable override
$env:TEST_Headless="true"; dotnet test Tests/Tests.csproj

# Stop SUT services (preserves SQLite database volume 'productapi-data')
docker compose down

# Stop SUT and reset database volume
docker compose down -v
```

---

## Package & Dependency Management

- **Central Package Management (CPM)** is enforced across the solution via `Directory.Packages.props` (`CentralPackageTransitivePinningEnabled = true`).
- **Never add a `Version` attribute** to a `PackageReference` in individual `.csproj` files.
- To add or upgrade a NuGet package:
  1. Add or update the package version in `Directory.Packages.props` under `<PackageVersion Include="..." Version="..." />`.
  2. Reference the package in the target `.csproj` with `<PackageReference Include="..." />`.

---

## Coding Style & Standards

Follow `.editorconfig` across all files:
- **C#**: 4 spaces indentation, file-scoped namespaces (`namespace Foo.Bar;`), LF line endings, nullable reference types enabled, implicit usings enabled, braces on new lines.
- **Naming**:
  - `PascalCase` for types, public members, and methods.
  - `_camelCase` for private fields.
  - `I`-prefix for interfaces (e.g., `IPlaywright`, `IPage`).
- **Markdown / Config**: 2 spaces indentation, LF line endings, UTF-8.

---

## Test Automation Framework Guidelines

### 1. Playwright Resource Lifecycle
- `Framework/Driver/PlaywrightDriver.cs` is the single source of truth for Playwright lifecycles. It is injected by Reqnroll's DI container (one instance per scenario).
- **Browser access**: Use `playwrightDriver.Page` (`IPage`). Do not instantiate browsers or pages directly.
- **API access**: Use `playwrightDriver.ApiRequestContext` (`Task<IAPIRequestContext>`).
- **HAR recording**: Every browser context automatically records HAR logs for `**/Product/**` routes saved as `sut-{guid}.har` next to the test DLL.
- **Lazy Initialization**: Resources (`IPlaywright`, `IBrowser`, `IBrowserContext`, `IPage`, `IAPIRequestContext`) are initialized lazily. API-only (`@api`) scenarios never spin up a browser.

### 2. Page Objects
- All page objects must extend `Framework.Driver.BasePage`.
- Use primary constructors to inject `PlaywrightDriver`.
- Leverage semantic ARIA locator helpers on `BasePage`:
  - `Link(string name)` -> `Page.GetByRole(AriaRole.Link, new() { Name = name })`
  - `Button(string name)` -> `Page.GetByRole(AriaRole.Button, new() { Name = name })`
  - `Row(string name)` -> `Page.GetByRole(AriaRole.Row, new() { Name = name })`
  - `Text(string text)` -> `Page.GetByText(text, new() { Exact = true })`
  - `ClickLinkAsync(string name)` and `ClickButtonAsync(string name)`
- Prefer ARIA role locators (`GetByRole`, `GetByLabel`) over CSS/XPath selectors. Reserve CSS selectors for elements lacking accessible semantics (e.g., `#Price`).

### 3. Step Definitions & Inter-Step State
- Mark step classes `sealed` and annotate with `[Binding]`.
- Inject dependencies (`ScenarioContext`, page objects, helpers) via C# 12 primary constructors.
- Pass state between steps using `ScenarioContext.Set<T>(value)` and `ScenarioContext.Get<T>()` / `ScenarioContext.TryGetValue<T>(out var val)`. Do not store scenario state in step class instance fields.
- **Strict step matching**: `Tests/reqnroll.json` sets `runtime.missingOrPendingStepsOutcome` to `Error`. A step with no binding, or one still marked pending, **fails the test run** (non-zero exit code) instead of being reported as skipped. There is no silent-skip path anymore.
  - Register a step with `[StepDefinition("...")]`, not `[Given(...)]`/`[When(...)]`/`[Then(...)]`, whenever the same text is used under more than one keyword in feature files — otherwise only that one keyword binds and the others now error.
  - Treat `Skipped:` greater than `0` in the `dotnet test` summary as a failure signal to investigate.

### 4. Mandatory Scenario Tags
Every scenario must carry:
1. Exactly one **test-type tag**: `@ui` or `@api`.
2. At least one **environment tag**: `@local`, `@dev`, `@qa`, `@stage`, or `@prod`.
Tag enforcement occurs in `BaseHook.VerifyMandatoryScenarioTags` at `Order = int.MinValue`.

### 5. Test Isolation & Cleanup
- Scenarios must be fully independent and runnable in parallel (`ExecutionScope.MethodLevel` is enabled).
- **Pre-execution cleanup**: Use a `Background:` step with `Given I ensure "{name}" data is cleaned up if already exists` before creating products.
- **Post-execution cleanup**: `Hooks.DeleteCreatedProduct` reads the created product from `ScenarioContext` and deletes it via `ProductsApi`.
- **Unique names**: `Tests/Support/ProductNames` suffixes every product a scenario creates with a per-scenario token, so parallel scenarios and concurrent runs can never share — or delete — each other's data. Steps call `Register(name)` when creating, `Actual(name)` / `ActualIn(message)` when asserting; seed-data names pass through untouched.

### 6. Assertions
- For UI tests: Use Playwright web-first assertions (`Microsoft.Playwright.Assertions.Expect(...)`) to benefit from auto-retries and timeouts.
- For API tests: Assert HTTP response statuses and deserialize JSON using `AwesomeAssertions` or MSTest asserts.

---

## SUT Integration & Contracts

### Endpoints & URLs
- WebApp: `http://localhost:5168` (calls ProductAPI at configured `ProductApi:BaseUrl` / `ProductApi__BaseUrl`).
- ProductAPI: `http://localhost:5044` (endpoints: `GET/POST/PUT/DELETE api/products`).
- Inside Docker Compose: WebApp reaches ProductAPI at `http://productapi:8080/`.

### Configuration Files
- `Tests/appsettings.json` - local runs: headed browser, `slowMo` 500.
- `Tests/appsettings.CI.json` - CI/CD runs: headless browser, `slowMo` 0.
- `ConfigReader` selects the CI file automatically when the `CI` environment variable is `true` (set by GitHub Actions and most CI servers); otherwise it loads the local file. A missing CI file falls back to the local one.

### Configuration Overrides
Test endpoints and execution parameters can be overridden with `TEST_<Setting>` environment variables (e.g., `TEST_Headless=true`, `TEST_ApplicationUrl=http://localhost:5168`, `TEST_APIRequestNewContextOptions__BaseURL=http://localhost:5044/api/`). Overrides are applied after the config file is loaded, so they win over both files; nested settings are addressed by their leaf name (`TEST_Headless`) or by a `__`-separated path of C# property names.

### ProductType Enum Consistency Note
`ProductType` is defined independently in three places — the test mirror plus two files in the SUT repository ([gromozeka1/sut-hardpoint](https://github.com/gromozeka1/sut-hardpoint)):
- SUT `ProductAPI/Models/ProductType.cs` (`CPU`, `MONITOR`, `PERIPHARALS`, `EXTERNAL`)
- `Tests/Models/Product.cs` (`CPU`, `MONITOR`, `PERIPHARALS`, `EXTERNAL`)
- SUT `WebApp/Models/ProductViewModel.cs` (`CPU`, `MONITOR`, `PERIPHERALS`, `EXTERNAL`)
Note the spelling difference (`PERIPHARALS` vs `PERIPHERALS`). Ensure all definitions remain aligned if updated — changing the SUT side requires a new image release and a `SUT_TAG` bump in `docker-compose.yml`.

---

## Commit & PR Guidelines
- Use imperative, concise commit messages (e.g., "Add test for product search and filtering").
- Do not commit machine-specific files, binaries (`bin/`, `obj/`), SQLite `.db` runtime files, screenshots, HAR traces, or report outputs (`TestReports.html`, `allure-results/`).
