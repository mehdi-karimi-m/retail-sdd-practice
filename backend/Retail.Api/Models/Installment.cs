using System.Text.Json.Serialization;
namespace Retail.Api.Models;
public sealed record Installment(int Number, [property: JsonNumberHandling(JsonNumberHandling.WriteAsString)] long AmountToman, string DueDate);
