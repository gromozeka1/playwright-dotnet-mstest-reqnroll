using Framework.Driver;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Fixtures;

public abstract class TestBase
{
    private static readonly ServiceProvider _rootProvider = BuildServiceProvider();
    private AsyncServiceScope _scope;

    // One scope per test keeps a single driver per test and isolates parallel tests.
    protected IServiceProvider Services => _scope.ServiceProvider;

    protected IPlaywrightDriver Driver => GetService<IPlaywrightDriver>();

    [TestInitialize]
    public void CreateScope() => _scope = _rootProvider.CreateAsyncScope();

    [TestCleanup]
    public async Task DisposeScopeAsync() => await _scope.DisposeAsync();

    protected T GetService<T>() where T : notnull => Services.GetRequiredService<T>();

    private static ServiceProvider BuildServiceProvider() => new ServiceCollection()
        .AddScoped<IPlaywrightDriver, PlaywrightDriver>()
        .BuildServiceProvider(validateScopes: true);
}
