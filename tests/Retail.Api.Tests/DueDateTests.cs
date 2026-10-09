using Retail.Api;
using Xunit;
namespace Retail.Api.Tests;
public sealed class DueDateTests
{
    [Theory]
    [InlineData("1405/07/16", "1405/08/16", "1405/09/16", "1405/10/16", "1405/11/16")]
    [InlineData("1405/06/31", "1405/07/30", "1405/08/30", "1405/09/30", "1405/10/30")]
    [InlineData("1404/11/30", "1404/12/29", "1405/01/30", "1405/02/30", "1405/03/30")]
    [InlineData("1403/11/30", "1403/12/30", "1404/01/30", "1404/02/30", "1404/03/30")]
    public void MonthsAreAddedIndependentlyFromBaseDate(string basis, string a, string b, string c, string d) =>
        Assert.Equal(new[] { a, b, c, d }, new PaymentPlanCalculator().GetDueDates(basis));

    [Theory]
    [InlineData("")] [InlineData("0000/01/01")] [InlineData("1404/12/30")] [InlineData("1405/07/31")]
    [InlineData("9999/12/30")]
    public void InvalidOrUnrepresentableDateCannotProduceFallback(string basis) =>
        Assert.ThrowsAny<ArgumentException>(() => new PaymentPlanCalculator().GetDueDates(basis));
}
