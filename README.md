# playwright-dotnet-mstest-reqnroll

Demo repository for a .NET browser and API test automation framework using Playwright and MSTest. The system under test (SUT) is a product-management application: an ASP.NET Core MVC WebApp backed by an ASP.NET Core ProductAPI and SQLite.

## Repository structure

- `Framework/` contains shared Playwright support, including browser and API request contexts, driver lifecycle, and test settings.
- `Tests/` contains Reqnroll feature files (`Features/`), step definitions (`Steps/`), hooks including Extent reporting (`Hooks/`), page objects, an API helper, and test configuration. UI scenarios are tagged `@ui`; API scenarios exercise ProductAPI through Playwright's API request context. Extent writes `TestReports.html` to the test output folder.
- `WebApp/` is the MVC user interface for viewing and managing products. It calls ProductAPI through `ProductApiClient`.
- `ProductAPI/` provides the product HTTP API and EF Core/SQLite persistence.
- `docker-compose.yml` and the service Dockerfiles define the local containerized SUT.
- `TestAutomation.slnx` groups the solution projects.

The solution targets .NET 10. Tests use Reqnroll with MSTest as the runner.

## Build and run tests

Install the .NET 10 SDK, then run these commands from the repository root:

```sh
dotnet build TestAutomation.slnx
pwsh Tests/bin/Debug/net10.0/playwright.ps1 install
dotnet test Tests/Tests.csproj
```

The `playwright.ps1` install step downloads the Playwright browsers and requires [PowerShell](https://learn.microsoft.com/powershell/scripting/install/installing-powershell) (`pwsh`). Run it after the first build and again whenever the Playwright package is updated; otherwise UI tests fail with "Looks like Playwright was just installed or updated. Please run the following command to download new browsers". To install only Chromium (the default browser), append `chromium`.

The test endpoints are configured in `Tests/appsettings.json`: `ApplicationUrl` is the WebApp URL and `ApplicationApiUrl` is the ProductAPI base URL used by API tests. Start the SUT before running tests. The defaults are `http://localhost:5168` for WebApp and `http://localhost:5044` for ProductAPI.

## Run the SUT with Docker Compose

Prerequisites: Docker Desktop or Docker Engine with Docker Compose v2, with the Docker daemon running. Run these commands from the repository root; ensure ports `5044` and `5168` are available.

Start both services and build their images:

```sh
docker compose up --build -d
```

The WebApp is available at `http://localhost:5168` and ProductAPI at `http://localhost:5044`. Inside the Compose network, WebApp connects to ProductAPI at `http://productapi:8080/`; the host URLs above are for accessing the services and tests from outside the containers.

The test settings in `Tests/appsettings.json` point the browser to `http://localhost:5168` (`ApplicationUrl`) and API requests to `http://localhost:5044/api/` (`ApplicationApiUrl`). Keep the Compose stack running while running tests with these settings.

Stop the services with:

```sh
docker compose down
```

ProductAPI data is stored in the `productapi-data` Docker volume and is preserved when the services are stopped this way.
