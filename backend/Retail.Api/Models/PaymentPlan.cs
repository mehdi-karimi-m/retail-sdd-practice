using System.Text.Json.Serialization;
namespace Retail.Api.Models;
public sealed record PaymentPlan(Product Product, string Currency, string Calendar, string TimeZone,
    string BaseDate, string ServerTime, string ExpiresAt, [property: JsonNumberHandling(JsonNumberHandling.WriteAsString)] long DownPaymentToman,
    IReadOnlyList<Installment> Installments, [property: JsonNumberHandling(JsonNumberHandling.WriteAsString)] long TotalPaymentToman, [property: JsonNumberHandling(JsonNumberHandling.WriteAsString)] long InterestToman, [property: JsonNumberHandling(JsonNumberHandling.WriteAsString)] long FeeToman);
