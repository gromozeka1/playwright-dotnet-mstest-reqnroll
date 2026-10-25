# Solution Architecture

## Projects

| Project | Role |
|---|---|
| `Framework/` | Shared test infrastructure: Playwright driver, config, base page/hook, ExtentReports helper, scenario tag constants |
| `Tests/` | Reqnroll/MSTest BDD tests: feature files, step definitions, page objects, hooks, support helpers, test models |
| `WebApp/` | ASP.NET Core MVC application — the UI under test |
| `ProductAPI/` | ASP.NET Core Web API — the API under test, EF Core + SQLite persistence |

Solution file: `TestAutomation.slnx` (.NET 10, `global.json` pins SDK to `10.0.100`).

## Dependency Flow

```
Tests  ──project ref──▶  Framework
Tests  ──HTTP (runtime)──▶  ProductAPI   (via ProductsApi helper + Playwright APIRequestContext)
WebApp ──HTTP (runtime)──▶  ProductAPI   (via ProductApiClient)
```

`Tests` and `WebApp` have no compile-time reference to `ProductAPI`. The test project mirrors the API's `Product` / `ProductType` models in `Tests/Models/` rather than sharing them.

## Key Design Patterns

### Lazy Playwright Object Graph
`PlaywrightDriver` initialises the full Playwright chain (`IPlaywright` → `IBrowser` → `IBrowserContext` → `IPage`) using `Lazy<Task<T>>`. API-only (`@api`) scenarios never launch a browser because the browser lazy chain is never accessed.

### Reqnroll Dependency Injection
All collaborators — `PlaywrightDriver`, `ExtentReportManager`, page objects, `ProductsApi` — are resolved from Reqnroll's built-in container. Step classes and page objects use C# 12 primary constructors throughout.

### ScenarioContext as Inter-Step State
Step classes pass domain objects between Given/When/Then steps via `ScenarioContext.Set<T>()` / `Get<T>()`. The cleanup hook (`Hooks.DeleteCreatedProduct`) also reads from `ScenarioContext` to know which product to remove.

### Test Isolation
- **Before:** `ReusableSteps.GivenIEnsureDataIsCleanedUpIfAlreadyExists` (used in `Background`) deletes a named product via the API before the scenario runs.
- **After:** `Hooks.DeleteCreatedProduct` (`[AfterScenario]`) deletes any product stored in `ScenarioContext` after every scenario.

### Parallelism
`[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]` is declared in `Framework/Driver/BaseHook.cs`, enabling MSTest to run all scenarios in parallel at the method level.

### HAR Recording
Every browser context records HAR for `**/Product/**` routes, saved next to the test DLL as `sut-{guid}.har`. This is always-on and requires no configuration.

## NuGet Package Management
All package versions are centralised in `Directory.Packages.props`. `.csproj` files list `PackageReference` without version attributes. `CentralPackageTransitivePinningEnabled` is `true`. Do not add version attributes to individual `PackageReference` elements.
