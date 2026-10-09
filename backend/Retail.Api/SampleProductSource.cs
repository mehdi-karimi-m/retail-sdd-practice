using Retail.Api.Models;
namespace Retail.Api;

public sealed record RawProduct(string? Name, decimal? PriceToman);
public interface IProductSource { RawProduct? GetProduct(); }

public sealed class SampleProductSource : IProductSource
{
    public RawProduct GetProduct() => new("محصول نمونه", 1000000m);

    public static Product Validate(RawProduct raw)
    {
        if (string.IsNullOrWhiteSpace(raw.Name) || raw.PriceToman is not decimal price ||
            price != decimal.Truncate(price) || price < 5m || price > 100000000000m)
            throw new ArgumentException("Product requires a name and an integer TOMAN price in the approved range.", nameof(raw));
        return new Product(raw.Name, checked((long)price));
    }
}
