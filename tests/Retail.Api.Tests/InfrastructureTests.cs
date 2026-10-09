using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Retail.Api.Tests;

public sealed class InfrastructureTests
{
    private static TestTimeProvider CreateClock() =>
        new(new DateTimeOffset(2026, 10, 8, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task UnknownRouteReturnsContractProblemWithoutErrors()
    {
        await using var factory = new ApiTestFactory(CreateClock());
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/does-not-exist", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        AssertProblem(json.RootElement, 404, "not_found");
        Assert.False(json.RootElement.TryGetProperty("errors", out _));
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Development")]
    public async Task ExceptionsReturnSanitizedProblem(string environment)
    {
        await using var factory = new ApiTestFactory(CreateClock(), services =>
            services.AddSingleton<IStartupFilter>(new FailureFilter()), environment);
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain("private exception detail", body);
        Assert.DoesNotContain("stackTrace", body);
        using var json = JsonDocument.Parse(body);
        AssertProblem(json.RootElement, 500, "internal_error");
    }

    [Theory]
    [InlineData(400, "invalid_request")]
    [InlineData(405, "method_not_allowed")]
    public async Task EmptyErrorStatusUsesContractProblem(int status, string code)
    {
        await using var factory = new ApiTestFactory(CreateClock(), services =>
            services.AddSingleton<IStartupFilter>(new StatusFilter(status)));
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/", TestContext.Current.CancellationToken);
        Assert.Equal(status, (int)response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        AssertProblem(json.RootElement, status, code);
    }

    [Fact]
    public async Task FactoryInjectsControllableClockAndAllowsServiceReplacement()
    {
        var clock = CreateClock();
        var marker = new object();
        await using var factory = new ApiTestFactory(clock, services => services.AddSingleton(marker));
        Assert.Same(marker, factory.Services.GetRequiredService<object>());
        Assert.Same(clock, factory.Services.GetRequiredService<TimeProvider>());
        Assert.Equal("Asia/Tehran", factory.Services.GetRequiredService<TimeZoneInfo>().Id);
        var start = clock.GetUtcNow();
        clock.Advance(TimeSpan.FromMinutes(1));
        Assert.Equal(start.AddMinutes(1), clock.GetUtcNow());
        Assert.Equal(2, clock.UtcNowReadCount);
        clock.SetUtcNow(start);
        clock.ResetReadCount();
        Assert.Equal(start, clock.GetUtcNow());
        Assert.Equal(1, clock.UtcNowReadCount);
    }

    private static void AssertProblem(JsonElement problem, int status, string code)
    {
        Assert.Equal("about:blank", problem.GetProperty("type").GetString());
        Assert.Equal(status, problem.GetProperty("status").GetInt32());
        Assert.Equal(code, problem.GetProperty("code").GetString());
        Assert.Matches("[\u0600-\u06ff]", problem.GetProperty("title").GetString()!);
        Assert.Matches("[\u0600-\u06ff]", problem.GetProperty("detail").GetString()!);
        Assert.False(problem.TryGetProperty("Status", out _));
    }

    private sealed class FailureFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            next(app);
            app.Run(context => throw new InvalidOperationException("private exception detail"));
        };
    }

    private sealed class StatusFilter(int status) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            next(app);
            app.Run(context =>
            {
                context.Response.StatusCode = status;
                return Task.CompletedTask;
            });
        };
    }
}
