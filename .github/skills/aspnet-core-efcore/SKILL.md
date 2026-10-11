---
name: aspnet-core-efcore
description: Use when implementing or changing the ProductAPI, WebApp, their HTTP contract, EF Core/SQLite persistence, or ASP.NET Core configuration in this repository.
---

# ASP.NET Core and EF Core in this repository

- Start from the existing project boundaries: `ProductAPI/` owns the `api/products` endpoints and `ProductContext`; `WebApp/` owns MVC routes/views and `ProductApiClient`.
- Inspect both the API controller and typed client before changing a route, payload, status code, or error behavior. Keep them compatible or update both in the same change.
- Keep SQLite database path and API base URL configurable through the established application configuration; do not hard-code developer-specific paths or addresses.
- Match existing async patterns and pass cancellation tokens to HTTP/EF operations when the call chain supports them.
- Add or update tests only in an existing test project, or create one only when explicitly required. Do not treat this solution's Framework library as a complete test suite.
- Build with `dotnet build TestAutomation.slnx`. If the change affects service integration, also validate the Compose configuration and run the services when Docker is available.
