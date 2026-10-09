namespace Retail.Api.Tests;

public sealed class TestTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    private DateTimeOffset current = utcNow.ToUniversalTime();
    public int UtcNowReadCount { get; private set; }

    public override DateTimeOffset GetUtcNow()
    {
        UtcNowReadCount++;
        return current;
    }

    public void SetUtcNow(DateTimeOffset value) => current = value.ToUniversalTime();
    public void Advance(TimeSpan elapsed) => current += elapsed;
    public void ResetReadCount() => UtcNowReadCount = 0;
}
