---
applyTo: "**/*.cs,**/*.csproj"
---

# .NET guidance

- Target the existing `net10.0` framework unless the task explicitly changes supported runtimes.
- Central Package Management is enforced via `Directory.Packages.props`. Do not declare package versions in `.csproj` files.
- Use the project's existing nullable, implicit-using, and formatting conventions (4 spaces, LF endings, file-scoped namespaces). Keep public APIs consistent with neighboring code.
- In `HardPoint.Testing`, everything public is consumer-facing: `PlaywrightDriver`, `BasePage`, `BaseHook`, `ConfigReader`/`TestSettings`, `ExtentReportManager`, `ArtifactDirectory`, `ScenarioTags`. A change to a binding signature, a config key or the tag vocabulary needs a version bump and an update to the packed `HardPoint.Testing/README.md`.
- Keep the package free of application specifics: no page objects, step definitions, feature files, SUT models or `docker-compose` — those belong to the consuming suite at https://github.com/gromozeka1/hardpoint-tests.
- Never write artifacts next to the executing assembly (`Assembly.Location`); it is the NuGet cache for consumers. Route them through `ArtifactDirectory`.
- In `HardPoint.Testing.Tests`, cover configuration and artifact-path resolution. These tests mutate cached settings and `TEST_` environment variables, so they must be `[DoNotParallelize]` and restore state in `[TestCleanup]`.
- Verify with `dotnet build TestAutomation.slnx`, `dotnet test HardPoint.Testing.Tests/HardPoint.Testing.Tests.csproj`, `dotnet format TestAutomation.slnx --verify-no-changes`, and `dotnet pack HardPoint.Testing/HardPoint.Testing.csproj -c Release -o artifacts`.
