# Repository instructions

## Stack and layout

- This is a .NET 10 solution (`TestAutomation.slnx`) pinned with `global.json`, with nullable
  reference types and implicit usings enabled.
- Central Package Management (CPM) is enabled in `Directory.Packages.props`. Never specify
  `Version` attributes on `PackageReference` elements in `.csproj` files.
- `HardPoint.Testing/` is the packable library: driver lifecycle (`PlaywrightDriver`), base
  classes (`BasePage`, `BaseHook`), configuration (`ConfigReader`, `TestSettings`), Extent
  reporting (`ExtentReportManager`), artifact paths (`ArtifactDirectory`) and the tag vocabulary
  (`ScenarioTags`). Namespaces are `HardPoint.Testing.*`.
- `HardPoint.Testing.Tests/` holds the framework's unit tests: configuration resolution,
  environment overrides, and artifact-path resolution.
- The consuming suite lives in https://github.com/gromozeka1/hardpoint-tests and the SUT in
  https://github.com/gromozeka1/sut-hardpoint. Do not add suite, feature or SUT sources here.

## Architecture and layer boundaries

- The package is the reusable layer. Anything application-specific — page objects, step
  definitions, feature files, SUT models, docker-compose — belongs to the consuming suite.
- `HardPoint.Testing/README.md` is packed as the package readme and is the consumer
  documentation: configuration keys, `TEST_` overrides, the `[assembly: Parallelize]`
  requirement and the artifact layout. Update it with any contract change.
- Dependencies are limited to `Microsoft.Playwright`, `Reqnroll.MSTest` and `ExtentReports`;
  they are part of the package's public surface, so version bumps there are consumer-visible.
- Follow `.editorconfig`: C# uses 4 spaces, file-scoped namespaces, braces on new lines, and LF
  line endings.

## Test automation conventions

- Assembly-level attributes must stay in the consuming test assembly. Do not add
  `[assembly: Parallelize]` to the package — it would configure the package, not the consumer.
- Tag enforcement is opt-in through `enforceScenarioTags` (default `false`). Keep the package
  free of a hard-coded tagging policy; the consuming suite turns it on.
- Hooks must be safe under parallel execution: shared state (the Extent reporter, feature nodes)
  is guarded by a single static lock.
- Config failures must name the resolved path and the remedy; a missing file must not surface as
  `TypeInitializationException`.
- Artifacts go through `ArtifactDirectory` — never write next to the assembly location, which is
  the NuGet cache for package consumers.

## Build and verification

- Build with `dotnet build TestAutomation.slnx`.
- Run unit tests with `dotnet test HardPoint.Testing.Tests/HardPoint.Testing.Tests.csproj`.
- Verify formatting with `dotnet format TestAutomation.slnx --verify-no-changes`.
- Verify packaging with `dotnet pack HardPoint.Testing/HardPoint.Testing.csproj -c Release -o artifacts`,
  then confirm the `.nupkg` and `.snupkg` are produced without warnings.
- Keep changes focused; do not reformat unrelated files or commit `bin/`, `obj/`, `*.nupkg`,
  `*.snupkg` or `artifacts/`.
