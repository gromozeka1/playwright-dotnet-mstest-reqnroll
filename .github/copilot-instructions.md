# Repository instructions

## Stack and layout

- This is a .NET 10 solution (`TestAutomation.slnx`), with nullable reference types and implicit usings enabled.
- `ProductAPI/` is an ASP.NET Core Web API backed by EF Core and SQLite. Its database file is created under `ProductAPI/Database/`.
- `WebApp/` is an ASP.NET Core MVC app that calls ProductAPI through the typed `ProductApiClient`. Its API base URL is configured by `ProductApi:BaseUrl` (environment variable: `ProductApi__BaseUrl`).
- `Framework/` is shared Playwright .NET/MSTest test-framework support. The solution currently has no dedicated test project or Reqnroll package; do not assume either exists or add either unless the task calls for it.
- `docker-compose.yml` runs ProductAPI and WebApp. The API is reachable at `http://localhost:5044`, and the web app at `http://localhost:5168`.

## Change guidance

- Follow the existing C# and `.editorconfig` conventions; keep nullable annotations meaningful and prefer async APIs for I/O.
- Keep responsibilities in their existing layers: MVC controllers and views in `WebApp/`, HTTP integration in `WebApp/Services/ProductApiClient.cs`, API endpoints in `ProductAPI/Controllers/`, and persistence in `ProductAPI/Models/`.
- Preserve the API routes and response semantics consumed by `ProductApiClient`; coordinate changes to both sides when a contract changes.
- Keep configuration environment-driven. Do not commit secrets, machine-specific settings, or generated `bin/` and `obj/` output.
- Preserve the Docker Compose service names and internal service-to-service address (`http://productapi:8080/`) when changing container configuration. Keep persistent API database data in the named `productapi-data` volume.
- Avoid adding dependencies, projects, migrations, or new architectural layers unless they are needed for the requested change.

## Build and verification

- Build the solution with `dotnet build TestAutomation.slnx`.
- Run `docker compose config` after changing Compose configuration, and build/run the relevant service or services when practical.
- Do not report browser tests as run unless an actual test project and tests are present and were executed. For Playwright changes, verify the affected framework code builds and describe any browser/runtime prerequisite that prevented a test run.
- Keep changes focused; do not reformat unrelated files.
