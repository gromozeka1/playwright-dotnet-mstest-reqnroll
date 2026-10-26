# CLAUDE.md - Assistant Guidelines

## Solution Overview
This repository is a .NET 10 solution (`TestAutomation.slnx`) containing a shared Playwright automation framework and an end-to-end BDD test suite using Reqnroll and MSTest. The system under test — an ASP.NET Core MVC WebApp (`localhost:5168`) backed by an ASP.NET Core REST API (`localhost:5044`) — is **not** part of this repository: it runs from published Docker Hub images (`gromozeka1/hardpoint-webapp`, `gromozeka1/hardpoint-productapi`), whose source lives in [gromozeka1/sut-hardpoint](https://github.com/gromozeka1/sut-hardpoint).

### Projects
- `Framework/`: Shared Playwright driver infrastructure, lifecycle management, reporting, and base classes (`BasePage`, `BaseHook`).
- `Tests/`: BDD tests with Reqnroll and MSTest. Contains `.feature` files, step definitions, page objects, and API test helpers.
- `docker-compose.yml`: Runs the SUT images from Docker Hub (version pinned by `SUT_TAG`, default `1.0.0`).

---

## Essential Commands

### Build & Setup
- `dotnet build TestAutomation.slnx` - Builds all projects in the solution.
- `pwsh Tests/bin/Debug/net10.0/playwright.ps1 install chromium` - Downloads Playwright browser binaries (run once after build).

### SUT Execution (Docker Compose)
- `docker compose up -d` - Pulls and launches the SUT images from Docker Hub (`productapi` port 5044, `webapp` port 5168).
- `docker compose down` - Stops services, preserving SQLite data volume.
- `docker compose down -v` - Stops services and clears data volume.

### Test Execution
- `dotnet test Tests/Tests.csproj` - Executes Reqnroll/MSTest suite against a running SUT.
- `$env:TEST_Headless="true"; dotnet test Tests/Tests.csproj` - Runs UI tests in headless mode.
- Report output: `TestReports.html` (ExtentReports) and Allure artifacts generated in test output.

---

## Key Development Rules

### Central Package Management (CPM)
- Package versions are strictly managed in `Directory.Packages.props`.
- **Never** add a `Version` attribute to a `<PackageReference>` in individual `.csproj` files.

### C# & Code Style (.editorconfig)
- Indentation: 4 spaces for C#, 2 spaces for JSON/Markdown/YAML.
- Line Endings: Strictly LF across all files.
- Namespaces: File-scoped namespaces (`namespace Project.SubNamespace;`).
- Nullable reference types: Enabled; preserve meaningful annotations.
- Constructors: Use C# 12 primary constructors where suitable.

### Test Automation Conventions
1. **Mandatory Tags**: Every scenario must have:
   - One test-type tag: `@ui` or `@api`
   - One environment tag: `@local`, `@dev`, `@qa`, `@stage`, or `@prod`
2. **Page Objects**:
   - Must inherit from `Framework.Driver.BasePage`.
   - Prefer ARIA role locators (`Link()`, `Button()`, `Row()`, `Text()`, `GetByRole()`, `GetByLabel()`) over CSS.
3. **Step Definitions**:
   - Must be `sealed` and tagged with `[Binding]`.
   - Use primary constructor DI; store inter-step state in `ScenarioContext`.
   - **Strict step matching**: `Tests/reqnroll.json` sets `runtime.missingOrPendingStepsOutcome` to `Error`, so a step with no binding or a pending step **fails the test run** (non-zero exit code) instead of being reported as skipped. There is no silent-skip path.
     - Register a step with `[StepDefinition("...")]` rather than `[Given(...)]`/`[When(...)]`/`[Then(...)]` whenever the same text is used under multiple keywords in feature files, otherwise only that keyword binds and the others now error.
     - Treat `Skipped:` greater than `0` in the `dotnet test` summary as a failure signal to investigate.
4. **Test Isolation**:
   - Clean up state before tests using `Given I ensure "{name}" data is cleaned up if already exists`.
   - `[AfterScenario]` hook handles post-test cleanup of created entities.
5. **Assertions**:
   - For UI: Use Playwright web-first assertions (`Assertions.Expect(...)`).
   - For API: Assert status codes and deserialize responses with `AwesomeAssertions`.
