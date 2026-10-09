using Retail.Api.Models;
namespace Retail.Api;
public sealed record RawProduct(string? Name, decimal? PriceToman);
public interface IProductSource { RawProduct? GetProduct(); }
public sealed class SampleProductSource : IProductSource
{
    public RawProduct? GetProduct() => throw new NotImplementedException("T020: product source");
    public static Product Validate(RawProduct raw) => throw new NotImplementedException("T020: numeric validation");
}
