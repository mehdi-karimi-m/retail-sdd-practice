using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Retail.Api.Tests;

public sealed class ApiTestFactory(
    TestTimeProvider clock,
    Action<IServiceCollection>? configureServices = null,
    string environment = "Production") : WebApplicationFactory<Program>
{
    public TestTimeProvider Clock { get; } = clock;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
            // Source/service replacements use DI here once their Phase 3 types exist.
            configureServices?.Invoke(services);
        });
    }
}
