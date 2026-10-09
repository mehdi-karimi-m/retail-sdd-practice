using Retail.Api;
using Retail.Api.Models;
using Xunit;

namespace Retail.Api.Tests;

public sealed class CalculationTests
{
    [Theory]
    [MemberData(nameof(AcceptanceFixtures.Money), MemberType = typeof(AcceptanceFixtures))]
    public void FixedExamplesPreserveAllFiveAmountsAndExactTotal(long price, long down, long basis, long last)
    {
        var product = new Product("محصول نمونه", price);
        var plan = new PaymentPlanCalculator().Calculate(product, "1405/07/16");
        Assert.Equal(down, plan.DownPaymentToman);
        Assert.Equal(new long[] { basis, basis, basis, last }, plan.Installments.Select(x => x.AmountToman));
        Assert.Equal(new[] { 1, 2, 3, 4 }, plan.Installments.Select(x => x.Number));
        Assert.All(plan.Installments, x => Assert.True(x.AmountToman > 0));
        Assert.Equal(price, plan.DownPaymentToman + plan.Installments.Sum(x => x.AmountToman));
        Assert.Equal(price, plan.TotalPaymentToman);
        Assert.Equal(0, plan.InterestToman);
        Assert.Equal(0, plan.FeeToman);
        Assert.InRange(last - basis, 0, 3);
    }

    [Theory]
    [InlineData(5L)] [InlineData(100000000000L)]
    public void InclusiveNumericBoundsAreAccepted(long price)
    {
        var product = SampleProductSource.Validate(new RawProduct("محصول نمونه", price));
        Assert.Equal(price, product.PriceToman);
        Assert.Equal("محصول نمونه", product.Name);
    }

    [Theory]
    [MemberData(nameof(AcceptanceFixtures.InvalidPrices), MemberType = typeof(AcceptanceFixtures))]
    public void InvalidDecimalOrMissingPriceIsRejectedBeforeConversion(decimal? price) =>
        Assert.Throws<ArgumentException>(() => SampleProductSource.Validate(new RawProduct("محصول نمونه", price)));

    [Theory]
    [InlineData(null)] [InlineData("")] [InlineData("   ")]
    public void MissingOrBlankNameIsRejected(string? name) =>
        Assert.Throws<ArgumentException>(() => SampleProductSource.Validate(new RawProduct(name, 1000000m)));
}
