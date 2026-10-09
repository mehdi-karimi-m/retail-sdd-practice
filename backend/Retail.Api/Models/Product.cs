using System.Text.Json.Serialization;
namespace Retail.Api.Models;
public sealed record Product(string Name, [property: JsonNumberHandling(JsonNumberHandling.WriteAsString)] long PriceToman)
{
    public string Currency => "TOMAN";
}
