# Repository Guidelines

## Project Structure & Module Organization

This .NET 10 solution contains four projects. `Framework/` holds shared Playwright drivers, configuration, and reporting helpers. `Tests/` contains Reqnroll feature files in `Features/`, step definitions in `Steps/`, page objects in `Pages/`, hooks, and test settings. `WebApp/` is the ASP.NET Core MVC product UI; `ProductAPI/` is its API and SQLite-backed persistence. `docker-compose.yml` starts the two services for local end-to-end runs. Generated feature code and test reports under `Tests/` are build outputs; edit the `.feature` files and source instead.

## Build, Test, and Development Commands

Run commands from the repository root with the .NET 10 SDK selected by `global.json`.

- `dotnet build TestAutomation.slnx` builds all projects.
- `pwsh Tests/bin/Debug/net10.0/playwright.ps1 install chromium` installs the browser used by UI tests after building.
- `docker compose up --build -d` starts WebApp (`localhost:5168`) and ProductAPI (`localhost:5044`); `docker compose down` stops them.
- `dotnet test Tests/Tests.csproj` runs the MSTest/Reqnroll suite. Start the SUT first; test URLs and `TEST_...` environment overrides are documented in `Tests/appsettings.json` and the root README.

## Coding Style & Naming Conventions

Follow `.editorconfig`: C# uses four spaces, file-scoped namespaces, braces on new lines, nullable reference types, and `using` directives outside namespaces. Use PascalCase for types and public members, `I`-prefixed interfaces, and `_camelCase` for private fields. Keep feature files in Gherkin under `Tests/Features/`, with matching step definitions in `Tests/Steps/` and UI selectors/actions encapsulated by page objects in `Tests/Pages/`.

## Testing Guidelines

Add or update Reqnroll scenarios in `.feature` files and implement reusable bindings in step classes. Tag browser scenarios `@ui`; keep API scenarios focused on the ProductAPI. Run `dotnet test Tests/Tests.csproj` against a running local or Compose SUT. The CI workflow builds, installs Chromium, starts Compose services, and runs tests headlessly.

## Commit & Pull Request Guidelines

Recent commits use short, descriptive imperative subjects (for example, “Fix Playwright resource lifecycle”); include an issue number when relevant. PRs should explain the behavior change, note relevant test coverage and results, link related issues, and include screenshots for visible UI changes. Keep generated reports and build artifacts out of source changes.

## Configuration & Secrets

Use `Tests/appsettings.json` for local endpoint defaults and environment variables such as `TEST_Headless=true` for overrides. Do not commit credentials, tokens, or machine-specific secrets; keep service URLs aligned with the local Compose ports or CI configuration.
