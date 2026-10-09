using System.Globalization;
using Retail.Api.Models;
namespace Retail.Api;

public class PaymentPlanCalculator
{
    public virtual PaymentPlan Calculate(Product product, string baseDate)
    {
        _ = SampleProductSource.Validate(new RawProduct(product.Name, product.PriceToman));
        checked
        {
            var down = product.PriceToman * 30 / 100;
            var remaining = product.PriceToman - down;
            var basis = remaining / 4;
            var last = remaining - 3 * basis;
            var dates = GetDueDates(baseDate);
            var installments = Enumerable.Range(1, 4)
                .Select(i => new Installment(i, i == 4 ? last : basis, dates[i - 1])).ToArray();
            var total = down + installments.Sum(x => x.AmountToman);
            if (down <= 0 || basis <= 0 || last <= 0 || last - basis is < 0 or > 3 || total != product.PriceToman)
                throw new InvalidOperationException("Payment invariants failed.");
            return new PaymentPlan(product, "TOMAN", "persian", "Asia/Tehran", baseDate,
                "", "", down, installments, total, 0, 0);
        }
    }

    public virtual IReadOnlyList<string> GetDueDates(string baseDate)
    {
        var date = TehranDateProvider.ParsePersianDate(baseDate);
        var calendar = new PersianCalendar();
        return Enumerable.Range(1, 4).Select(i => TehranDateProvider.FormatPersianDate(calendar.AddMonths(date, i))).ToArray();
    }
}
