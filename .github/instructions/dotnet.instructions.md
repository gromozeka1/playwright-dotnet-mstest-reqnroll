---
applyTo: "**/*.cs,**/*.csproj"
---

# .NET guidance

- Target the existing `net10.0` framework unless the task explicitly changes supported runtimes.
- Use the project's existing nullable, implicit-using, and formatting conventions. Keep public APIs and dependency injection registrations consistent with neighboring code.
- In ProductAPI, keep database access on `ProductContext`/EF Core and preserve the `api/products` contract used by WebApp. Handle expected missing resources explicitly; do not hide unexpected failures behind success responses.
- In WebApp, use the registered typed `ProductApiClient` for ProductAPI calls and pass cancellation tokens through HTTP operations when available. Keep the API base URL configurable via `ProductApi:BaseUrl`.
- In Framework, use the existing Playwright .NET and MSTest packages and helpers. Do not introduce Reqnroll assumptions or test-runner setup without an explicit requirement.
- Add or update focused tests when a relevant test project exists. The current solution does not contain a dedicated test project; do not claim `dotnet test` validated a behavior unless tests actually ran.
