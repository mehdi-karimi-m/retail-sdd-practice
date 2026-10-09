using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Retail.Api;
using Xunit;
namespace Retail.Api.Tests;

public sealed class ApiTests
{
    [Fact]
    public void CommittedOpenApiExamplesValidateWithTestAssertionUtility()
    {
        using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "openapi.json")));
        var paths = document.RootElement.GetProperty("paths");
        foreach (var (path, method, schema) in new[]
        {
            ("/api/product", "get", "Product"),
            ("/api/product/payment-plan", "get", "PaymentPlan"),
            ("/api/product/payment-plan/validity", "post", "ValidityResponse")
        })
            ContractAssert.Schema(paths.GetProperty(path).GetProperty(method).GetProperty("responses")
                .GetProperty("200").GetProperty("content").GetProperty("application/json").GetProperty("example"), schema);
    }
    private static ApiTestFactory Factory(RawProduct? product, TestTimeProvider? clock = null,
        AcceptanceFixtures.SpyCalculator? calculator = null) => new(clock ?? AcceptanceFixtures.Clock(), services =>
        {
            services.RemoveAll<IProductSource>();
            services.AddSingleton<IProductSource>(new AcceptanceFixtures.Source(product));
            if (calculator is not null)
            { services.RemoveAll<PaymentPlanCalculator>(); services.AddSingleton<PaymentPlanCalculator>(calculator); }
        });

    [Theory]
    [MemberData(nameof(AcceptanceFixtures.Money), MemberType = typeof(AcceptanceFixtures))]
    public async Task BothAnonymousGetsMatchSchemaAndFixedMoney(long price, long down, long basis, long last)
    {
        var clock = AcceptanceFixtures.Clock();
        await using var factory = Factory(new RawProduct("محصول نمونه", price), clock);
        using var client = factory.CreateClient();
        using var productResponse = await client.GetAsync("/api/product", TestContext.Current.CancellationToken);
        var product = await ContractAssert.Json(productResponse, 200, "Product");
        Assert.Equal(price.ToString(CultureInfo.InvariantCulture), product.GetProperty("priceToman").GetString());
        using var response = await client.GetAsync("/api/product/payment-plan", TestContext.Current.CancellationToken);
        var plan = await ContractAssert.Json(response, 200, "PaymentPlan");
        Assert.Equal(product.GetRawText(), plan.GetProperty("product").GetRawText());
        Assert.Equal(down.ToString(CultureInfo.InvariantCulture), plan.GetProperty("downPaymentToman").GetString());
        var installments = plan.GetProperty("installments").EnumerateArray().ToArray();
        Assert.Equal(new[] { 1, 2, 3, 4 }, installments.Select(x => x.GetProperty("number").GetInt32()));
        Assert.Equal(new[] { basis, basis, basis, last }, installments.Select(x => long.Parse(x.GetProperty("amountToman").GetString()!, CultureInfo.InvariantCulture)));
        Assert.Equal(price, down + installments.Sum(x => long.Parse(x.GetProperty("amountToman").GetString()!, CultureInfo.InvariantCulture)));
        Assert.Equal(price.ToString(CultureInfo.InvariantCulture), plan.GetProperty("totalPaymentToman").GetString());
        Assert.Equal("0", plan.GetProperty("interestToman").GetString()); Assert.Equal("0", plan.GetProperty("feeToman").GetString());
        Assert.Equal("2026-10-08T12:00:00Z", plan.GetProperty("serverTime").GetString());
        Assert.Equal("2026-10-08T20:30:00Z", plan.GetProperty("expiresAt").GetString());
        Assert.Equal("1405/07/16", plan.GetProperty("baseDate").GetString());
        Assert.Equal(1, clock.UtcNowReadCount);
    }

    [Theory]
    [MemberData(nameof(AcceptanceFixtures.InvalidPrices), MemberType = typeof(AcceptanceFixtures))]
    public async Task InvalidSourcePriceIsServerError(decimal? price)
    {
        await using var factory = Factory(new RawProduct("محصول نمونه", price)); using var client = factory.CreateClient();
        foreach (var path in new[] { "/api/product", "/api/product/payment-plan" })
        { using var r = await client.GetAsync(path, TestContext.Current.CancellationToken); await ContractAssert.Problem(r, 500, "invalid_product"); }
    }
    [Theory]
    [InlineData(null)] [InlineData("")] [InlineData("   ")]
    public async Task InvalidNameIsServerError(string? name)
    {
        await using var factory = Factory(new RawProduct(name, 1000000m)); using var client = factory.CreateClient();
        foreach (var path in new[] { "/api/product", "/api/product/payment-plan" })
        { using var r = await client.GetAsync(path, TestContext.Current.CancellationToken); await ContractAssert.Problem(r, 500, "invalid_product"); }
    }
    [Fact]
    public async Task MissingSourceProductReturns404()
    {
        await using var factory = Factory(null); using var client = factory.CreateClient();
        foreach (var path in new[] { "/api/product", "/api/product/payment-plan" })
        { using var r = await client.GetAsync(path, TestContext.Current.CancellationToken); await ContractAssert.Problem(r, 404, "product_not_found"); }
    }
    [Fact]
    public async Task CalculatorFailureCannotReturnPartialPlan()
    {
        var spy = new AcceptanceFixtures.SpyCalculator();
        await using var factory = Factory(new RawProduct("محصول نمونه", 1000000m), calculator: spy);
        using var client = factory.CreateClient(); using var r = await client.GetAsync("/api/product/payment-plan", TestContext.Current.CancellationToken);
        await ContractAssert.Problem(r, 500, "calculation_failed"); Assert.Equal(1, spy.Calls);
    }
    [Theory]
    [InlineData("/api/product")] [InlineData("/api/product/payment-plan")]
    public async Task QueryParametersCannotOverrideServerData(string path)
    {
        await using var factory = Factory(new RawProduct("محصول نمونه", 1000000m)); using var client = factory.CreateClient();
        using var baseline = await client.GetAsync(path, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, baseline.StatusCode);
        var body = await baseline.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        foreach (var query in new[] { "priceToman=5", "baseDate=1400/01/01", "foo=bar" })
        { using var r = await client.GetAsync(path + "?" + query, TestContext.Current.CancellationToken);
          Assert.Equal(HttpStatusCode.OK, r.StatusCode); Assert.Equal(body, await r.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)); }
    }
    [Theory]
    [InlineData("/api/product")] [InlineData("/api/product/payment-plan")]
    public async Task GetBodyAndWrongMethodAreRejected(string path)
    {
        await using var factory = Factory(new RawProduct("محصول نمونه", 1000000m)); using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, path) { Content = new StringContent("{}", Encoding.UTF8, "application/json") };
        using var body = await client.SendAsync(request, TestContext.Current.CancellationToken); await ContractAssert.Problem(body, 400, "invalid_request");
        using var post = await client.PostAsync(path, null, TestContext.Current.CancellationToken); await ContractAssert.Problem(post, 405, "method_not_allowed");
    }
    [Fact]
    public async Task NewRequestAcrossMidnightGetsNewDayWithoutCaching()
    {
        var clock = AcceptanceFixtures.Clock("2026-10-08T20:29:59Z");
        await using var factory = Factory(new RawProduct("محصول نمونه", 1000000m), clock); using var client = factory.CreateClient();
        using var before = await client.GetAsync("/api/product/payment-plan", TestContext.Current.CancellationToken);
        var a = await ContractAssert.Json(before, 200, "PaymentPlan"); clock.Advance(TimeSpan.FromSeconds(1));
        using var after = await client.GetAsync("/api/product/payment-plan", TestContext.Current.CancellationToken);
        var b = await ContractAssert.Json(after, 200, "PaymentPlan");
        Assert.Equal("1405/07/16", a.GetProperty("baseDate").GetString()); Assert.Equal("1405/07/17", b.GetProperty("baseDate").GetString());
        Assert.Equal("2026-10-09T20:30:00Z", b.GetProperty("expiresAt").GetString()); Assert.Equal(2, clock.UtcNowReadCount);
    }
    [Fact]
    public async Task UnknownRouteHasNoPartialPlan()
    {
        await using var factory = Factory(null); using var client = factory.CreateClient();
        using var r = await client.GetAsync("/api/unknown", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
        using var json = System.Text.Json.JsonDocument.Parse(await r.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        ContractAssert.Schema(json.RootElement, "ProblemDetails"); Assert.Equal("not_found", json.RootElement.GetProperty("code").GetString());
    }
}
