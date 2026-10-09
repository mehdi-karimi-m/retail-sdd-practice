namespace Retail.Api.Models;
public sealed record PaymentPlan(Product Product, string Currency, string Calendar, string TimeZone,
    string BaseDate, string ServerTime, string ExpiresAt, long DownPaymentToman,
    IReadOnlyList<Installment> Installments, long TotalPaymentToman, long InterestToman, long FeeToman);
