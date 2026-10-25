---
applyTo: "**/*.cs,**/*.csproj"
---

# .NET guidance

- Target the existing `net10.0` framework unless the task explicitly changes supported runtimes.
- Central Package Management is enforced via `Directory.Packages.props`. Do not declare package versions in `.csproj` files.
- Use the project's existing nullable, implicit-using, and formatting conventions (4 spaces, LF endings, file-scoped namespaces). Keep public APIs and dependency injection registrations consistent with neighboring code.
- In ProductAPI, keep database access on `ProductContext`/EF Core and preserve the `api/products` contract used by WebApp. Handle expected missing resources explicitly; do not hide unexpected failures behind success responses.
- In WebApp, use the registered typed `ProductApiClient` for ProductAPI calls and pass cancellation tokens through HTTP operations when available. Keep the API base URL configurable via `ProductApi:BaseUrl`.
- In Framework, provide reusable Playwright .NET and MSTest helpers, configuration parsing, base classes (`BasePage`, `BaseHook`), and Extent reporting.
- In Tests, implement Reqnroll BDD scenarios, step definitions, page objects, and lifecycle hooks using MSTest. Run tests using `dotnet test Tests/Tests.csproj` against the running SUT.
