# Repository instructions

## Stack and layout

- This is a .NET 10 solution (`TestAutomation.slnx`) pinned with `global.json`, with nullable reference types and implicit usings enabled.
- Central Package Management (CPM) is enabled in `Directory.Packages.props`. Never specify `Version` attributes directly on `PackageReference` elements in `.csproj` files.
- `ProductAPI/` is an ASP.NET Core Web API backed by EF Core and SQLite (`Database/ProductList.db`).
- `WebApp/` is an ASP.NET Core MVC app that calls ProductAPI through the typed `ProductApiClient`. Its API base URL is configured by `ProductApi:BaseUrl` (environment variable: `ProductApi__BaseUrl`).
- `Framework/` provides shared Playwright test-framework support: driver lifecycle (`PlaywrightDriver`), base classes (`BasePage`, `BaseHook`), configuration reading (`ConfigReader`), and Extent reporting helpers (`ExtentReportManager`).
- `Tests/` is an automated test suite utilizing Reqnroll BDD and MSTest. It contains feature files (`Features/`), step definitions (`Steps/`), page objects (`Pages/`), hooks (`Hooks/`), and API helpers (`Support/`).
- `docker-compose.yml` runs ProductAPI and WebApp. The API is reachable locally at `http://localhost:5044`, and WebApp at `http://localhost:5168`. Within Compose, WebApp connects to `http://productapi:8080/`.

## Architecture and layer boundaries

- Keep responsibilities strictly in their layers:
  - MVC controllers and Razor views in `WebApp/`.
  - HTTP client integration in `WebApp/Services/ProductApiClient.cs`.
  - API endpoints and controllers in `ProductAPI/Controllers/`.
  - Persistence and EF Core entities in `ProductAPI/Models/`.
  - Reusable test drivers, hooks, and helpers in `Framework/`.
  - BDD features, step definitions, page objects, and test models in `Tests/`.
- `Tests` and `WebApp` have no compile-time reference to `ProductAPI`. The test project mirrors API models in `Tests/Models/Product.cs`.
- Preserve the API routes and response semantics consumed by `ProductApiClient` and `Tests/Support/ProductsApi.cs`. Coordinate changes to both sides when contract changes occur.
- Follow `.editorconfig`: C# uses 4 spaces, file-scoped namespaces, braces on new lines, and LF line endings.

## Test automation conventions

- Tag every Reqnroll scenario with:
  1. Exactly one test-type tag: `@ui` or `@api`.
  2. At least one environment tag: `@local`, `@dev`, `@qa`, `@stage`, or `@prod`.
- Page objects in `Tests/Pages/` must inherit from `Framework.Driver.BasePage`. Use semantic ARIA role locators (`Link()`, `Button()`, `Row()`, `Text()`, `GetByRole()`, `GetByLabel()`) before falling back to CSS selectors.
- Step classes in `Tests/Steps/` must be `sealed`, annotated with `[Binding]`, and use primary constructors. Store inter-step data in `ScenarioContext`.
- Ensure test isolation: Use `Background:` steps with `Given I ensure "{name}" data is cleaned up if already exists` before creating entities, and leverage the `[AfterScenario]` deletion hook.
- Use Playwright web-first assertions (`Assertions.Expect(...)`) for browser tests.

## Build and verification

- Build the solution with `dotnet build TestAutomation.slnx`.
- Browser binaries for Playwright can be installed via `pwsh Tests/bin/Debug/net10.0/playwright.ps1 install chromium`.
- Run tests with `dotnet test Tests/Tests.csproj` against the running SUT. Start SUT with `docker compose up --build -d`.
- Validate Compose configuration with `docker compose config`.
- Test settings and headless mode can be overridden using environment variables (e.g., `TEST_Headless=true`).
- Keep changes focused; do not reformat unrelated files or commit test report outputs (`TestReports.html`, `allure-results/`).
