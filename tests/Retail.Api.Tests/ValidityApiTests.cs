using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Retail.Api;
using Xunit;
namespace Retail.Api.Tests;
public sealed class ValidityApiTests
{
    private const string Path = "/api/product/payment-plan/validity";
    private static ApiTestFactory Factory(TestTimeProvider clock, AcceptanceFixtures.SpyCalculator spy,
        AcceptanceFixtures.Source source) => new(clock, services =>
        {
            services.RemoveAll<PaymentPlanCalculator>(); services.AddSingleton<PaymentPlanCalculator>(spy);
            services.RemoveAll<IProductSource>(); services.AddSingleton<IProductSource>(source);
        });
    private static StringContent Body(string day = "1405/07/16", string expiry = "2026-10-08T20:30:00Z") =>
        new($"{{\"baseDate\":\"{day}\",\"expiresAt\":\"{expiry}\"}}", Encoding.UTF8, "application/json");

    [Theory]
    [InlineData("2026-10-08T20:29:59Z", "1405/07/16", "2026-10-08T20:30:00Z", true, "2026-10-08T20:30:00Z")]
    [InlineData("2026-10-08T20:30:00Z", "1405/07/16", "2026-10-08T20:30:00Z", false, "2026-10-08T20:30:00Z")]
    [InlineData("2026-10-08T20:30:01Z", "1405/07/16", "2026-10-08T20:30:00Z", false, "2026-10-08T20:30:00Z")]
    [InlineData("2026-10-08T12:00:00Z", "1405/07/15", "2026-10-07T20:30:00Z", false, "2026-10-07T20:30:00Z")]
    [InlineData("2026-10-08T12:00:00Z", "1405/07/17", "2026-10-09T20:30:00Z", false, "2026-10-09T20:30:00Z")]
    [InlineData("2026-10-08T12:00:00Z", "1405/07/16", "2026-10-08T20:31:00Z", false, "2026-10-08T20:30:00Z")]
    public async Task ChecksPriorSnapshotOnly(string now, string day, string expiry, bool valid, string canonical)
    {
        var spy = new AcceptanceFixtures.SpyCalculator(); var source = new AcceptanceFixtures.Source(null);
        var clock = AcceptanceFixtures.Clock(now); await using var factory = Factory(clock, spy, source);
        using var client = factory.CreateClient(); using var content = Body(day, expiry);
        using var r = await client.PostAsync(Path, content, TestContext.Current.CancellationToken);
        var json = await ContractAssert.Json(r, 200, "ValidityResponse");
        Assert.Equal(new[] { "baseDate", "expiresAt", "isValid", "serverTime" }, json.EnumerateObject().Select(x => x.Name).Order());
        Assert.Equal(day, json.GetProperty("baseDate").GetString()); Assert.Equal(now, json.GetProperty("serverTime").GetString());
        Assert.Equal(canonical, json.GetProperty("expiresAt").GetString()); Assert.Equal(valid, json.GetProperty("isValid").GetBoolean());
        Assert.Equal(1, clock.UtcNowReadCount); Assert.Equal(0, spy.Calls); Assert.Equal(0, source.Reads);
    }

    [Theory]
    [InlineData("{")] [InlineData("{}")] [InlineData("null")]
    [InlineData("{\"baseDate\":\"1405/07/16\"}")]
    [InlineData("{\"expiresAt\":\"2026-10-08T20:30:00Z\"}")]
    [InlineData("{\"baseDate\":\"1405/07/31\",\"expiresAt\":\"2026-10-08T20:30:00Z\"}")]
    [InlineData("{\"baseDate\":\"1404/12/30\",\"expiresAt\":\"2026-10-08T20:30:00Z\"}")]
    [InlineData("{\"baseDate\":\"0000/01/01\",\"expiresAt\":\"2026-10-08T20:30:00Z\"}")]
    [InlineData("{\"baseDate\":\"1405/07/16\",\"expiresAt\":\"2026-10-09T00:00:00+03:30\"}")]
    [InlineData("{\"baseDate\":\"1405/07/16\",\"expiresAt\":\"bad\"}")]
    [InlineData("{\"baseDate\":\"1405/07/16\",\"expiresAt\":\"2026-10-08T20:30:00Z\",\"extra\":1}")]
    [InlineData("{\"baseDate\":1,\"expiresAt\":null}")]
    public async Task InvalidBodyIs400WithoutFinancialCalculation(string body)
    {
        var spy = new AcceptanceFixtures.SpyCalculator(); var source = new AcceptanceFixtures.Source(null);
        await using var factory = Factory(AcceptanceFixtures.Clock(), spy, source); using var client = factory.CreateClient();
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        using var r = await client.PostAsync(Path, content, TestContext.Current.CancellationToken);
        await ContractAssert.Problem(r, 400, "invalid_request"); Assert.Equal(0, spy.Calls); Assert.Equal(0, source.Reads);
    }

    [Fact]
    public async Task ClockFailureIs500NotPermissionToDisplayOldPlan()
    {
        var spy = new AcceptanceFixtures.SpyCalculator(); var source = new AcceptanceFixtures.Source(null);
        await using var factory = new ApiTestFactory(AcceptanceFixtures.Clock(), services =>
        {
            services.RemoveAll<TimeProvider>(); services.AddSingleton<TimeProvider>(new BrokenClock());
            services.AddSingleton<IProductSource>(source); services.AddSingleton<PaymentPlanCalculator>(spy);
        });
        using var client = factory.CreateClient(); using var content = Body();
        using var r = await client.PostAsync(Path, content, TestContext.Current.CancellationToken);
        await ContractAssert.Problem(r, 500, "internal_error"); Assert.Equal(0, spy.Calls); Assert.Equal(0, source.Reads);
    }
    [Fact]
    public async Task UnknownQueryDoesNotChangeValidityOrCalculatePlan()
    {
        var spy = new AcceptanceFixtures.SpyCalculator(); var source = new AcceptanceFixtures.Source(null);
        await using var factory = Factory(AcceptanceFixtures.Clock(), spy, source); using var client = factory.CreateClient();
        using var contentA = Body(); using var a = await client.PostAsync(Path, contentA, TestContext.Current.CancellationToken);
        var first = await ContractAssert.Json(a, 200, "ValidityResponse");
        using var contentB = Body(); using var b = await client.PostAsync(Path + "?priceToman=5&foo=bar", contentB, TestContext.Current.CancellationToken);
        var second = await ContractAssert.Json(b, 200, "ValidityResponse");
        Assert.Equal(first.GetRawText(), second.GetRawText()); Assert.Equal(0, spy.Calls); Assert.Equal(0, source.Reads);
    }
    private sealed class BrokenClock : TimeProvider
    { public override DateTimeOffset GetUtcNow() => throw new InvalidOperationException("private clock failure"); }

    [Fact]
    public async Task UnrepresentableServerDateIs500RatherThanInvalidClientRequest()
    {
        await using var factory = new ApiTestFactory(new TestTimeProvider(DateTimeOffset.MinValue));
        using var client = factory.CreateClient(); using var content = Body();
        using var response = await client.PostAsync(Path, content, TestContext.Current.CancellationToken);
        await ContractAssert.Problem(response, 500, "internal_error");
    }

    [Fact]
    public async Task PersianDateWithTrailingNewlineIsInvalidRequest()
    {
        await using var factory = new ApiTestFactory(AcceptanceFixtures.Clock());
        using var client = factory.CreateClient();
        using var content = new StringContent("{\"baseDate\":\"1405/07/16\\n\",\"expiresAt\":\"2026-10-08T20:30:00Z\"}", Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(Path, content, TestContext.Current.CancellationToken);
        await ContractAssert.Problem(response, 400, "invalid_request");
    }

    [Fact]
    public async Task NonJsonContentTypeIsRejectedWithoutReadingClock()
    {
        var clock = AcceptanceFixtures.Clock();
        await using var factory = new ApiTestFactory(clock); using var client = factory.CreateClient();
        using var content = new StringContent("{\"baseDate\":\"1405/07/16\",\"expiresAt\":\"2026-10-08T20:30:00Z\"}", Encoding.UTF8, "text/plain");
        using var response = await client.PostAsync(Path, content, TestContext.Current.CancellationToken);
        await ContractAssert.Problem(response, 400, "invalid_request"); Assert.Equal(0, clock.UtcNowReadCount);
    }
}
