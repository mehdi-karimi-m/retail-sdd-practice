using Retail.Api;
using Xunit;
namespace Retail.Api.Tests;

[CollectionDefinition("Timezone environment", DisableParallelization = true)]
public sealed class TimezoneCollection;

[Collection("Timezone environment")]
public sealed class TehranDateTests
{
    [Theory]
    [InlineData("2026-10-08T12:00:00Z", "1405/07/16", "2026-10-08T20:30:00Z")]
    [InlineData("2026-10-08T20:29:59Z", "1405/07/16", "2026-10-08T20:30:00Z")]
    [InlineData("2026-10-08T20:30:00Z", "1405/07/17", "2026-10-09T20:30:00Z")]
    public void ServerClockDefinesDayAndNextMidnight(string utc, string day, string expiry)
    {
        var clock = AcceptanceFixtures.Clock(utc);
        var snapshot = new TehranDateProvider(clock, TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran")).GetSnapshot();
        Assert.Equal(day, snapshot.BaseDate);
        Assert.Equal(utc, snapshot.ServerTime);
        Assert.Equal(expiry, snapshot.ExpiresAt);
        Assert.Equal(1, clock.UtcNowReadCount);
    }

    [Theory]
    [InlineData("UTC")] [InlineData("America/Los_Angeles")]
    public void ProcessTimezoneCannotChangeTehranDay(string zone)
    {
        var old = Environment.GetEnvironmentVariable("TZ");
        try
        {
            Environment.SetEnvironmentVariable("TZ", zone); TimeZoneInfo.ClearCachedData();
            var provider = new TehranDateProvider(AcceptanceFixtures.Clock("2026-10-08T20:30:00Z"),
                TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran"));
            Assert.Equal("1405/07/17", provider.GetSnapshot().BaseDate);
        }
        finally { Environment.SetEnvironmentVariable("TZ", old); TimeZoneInfo.ClearCachedData(); }
    }

    [Fact]
    public void ChangingClockIsReadOnlyOnceForOneSnapshot()
    {
        var clock = new ChangingClock();
        var snapshot = new TehranDateProvider(clock, TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran")).GetSnapshot();
        Assert.Equal(1, clock.Reads);
        Assert.Equal("1405/07/16", snapshot.BaseDate);
        Assert.Equal("2026-10-08T20:29:59Z", snapshot.ServerTime);
        Assert.Equal("2026-10-08T20:30:00Z", snapshot.ExpiresAt);
    }

    [Fact]
    public void DateOutsidePersianRangeFailsRatherThanFallingBack()
    {
        var clock = new TestTimeProvider(DateTimeOffset.MinValue);
        Assert.ThrowsAny<ArgumentException>(() =>
            new TehranDateProvider(clock, TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran")).GetSnapshot());
    }

    private sealed class ChangingClock : TimeProvider
    {
        public int Reads { get; private set; }
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.Parse(
            ++Reads == 1 ? "2026-10-08T20:29:59Z" : "2026-10-08T20:30:00Z",
            global::System.Globalization.CultureInfo.InvariantCulture);
    }
}
