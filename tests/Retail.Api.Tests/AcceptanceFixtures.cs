using System.Globalization;
using Retail.Api;
using Retail.Api.Models;
using Xunit;

namespace Retail.Api.Tests;

public static class AcceptanceFixtures
{
    // Fixed expectations from test-design.md, never calculated by the subject under test.
    public static TheoryData<long, long, long, long> Money => new()
    {
        { 1000000, 300000, 175000, 175000 }, { 1000010, 300003, 175001, 175004 },
        { 1000001, 300000, 175000, 175001 }, { 5, 1, 1, 1 },
        { 100000000000, 30000000000, 17500000000, 17500000000 }
    };
    public static TheoryData<decimal?> InvalidPrices => new()
        { 1m, 2m, 3m, 4m, 0m, -1m, 5.5m, 100000000001m, (decimal?)null };
    public static TestTimeProvider Clock(string utc = "2026-10-08T12:00:00Z") =>
        new(DateTimeOffset.Parse(utc, CultureInfo.InvariantCulture));
    public sealed class Source(RawProduct? product) : IProductSource
    {
        public int Reads { get; private set; }
        public RawProduct? GetProduct() { Reads++; return product; }
    }
    public sealed class SpyCalculator : PaymentPlanCalculator
    {
        public int Calls { get; private set; }
        public override PaymentPlan Calculate(Product product, string baseDate)
        {
            Calls++;
            throw new OverflowException("private calculation failure");
        }
    }
}
