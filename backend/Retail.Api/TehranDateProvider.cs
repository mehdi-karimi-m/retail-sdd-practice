using Retail.Api.Models;
namespace Retail.Api;
public sealed record TehranSnapshot(string BaseDate, string ServerTime, string ExpiresAt);
public class TehranDateProvider
{
    public TehranDateProvider(TimeProvider clock, TimeZoneInfo timeZone) { }
    public virtual TehranSnapshot GetSnapshot() => throw new NotImplementedException("T023: Tehran clock snapshot");
    public virtual ValidityResponse CheckValidity(ValidityRequest request) =>
        throw new NotImplementedException("T023: prior snapshot validity");
}
