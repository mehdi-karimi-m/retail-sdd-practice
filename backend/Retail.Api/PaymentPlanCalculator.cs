using Retail.Api.Models;
namespace Retail.Api;
public class PaymentPlanCalculator
{
    public virtual PaymentPlan Calculate(Product product, string baseDate) =>
        throw new NotImplementedException("T021/T022: payment calculation");
    public virtual IReadOnlyList<string> GetDueDates(string baseDate) =>
        throw new NotImplementedException("T022: Persian due dates");
}
