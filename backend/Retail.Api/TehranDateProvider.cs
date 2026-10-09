using System.Globalization;
using System.Text.RegularExpressions;
using Retail.Api.Models;
namespace Retail.Api;

public sealed record TehranSnapshot(string BaseDate, string ServerTime, string ExpiresAt);
public class TehranDateProvider(TimeProvider clock, TimeZoneInfo timeZone)
{
    public virtual TehranSnapshot GetSnapshot()
    {
        var now = clock.GetUtcNow();
        var day = TimeZoneInfo.ConvertTime(now, timeZone).Date;
        return new TehranSnapshot(FormatPersianDate(day), FormatUtc(now), FormatUtc(Expiry(day)));
    }

    public virtual ValidityResponse CheckValidity(ValidityRequest request)
    {
        var day = ParsePersianDate(request.BaseDate);
        var supplied = ParseUtc(request.ExpiresAt);
        var canonical = Expiry(day);
        var snapshot = GetSnapshot();
        var valid = request.BaseDate == snapshot.BaseDate && supplied == canonical && ParseUtc(snapshot.ServerTime) < canonical;
        return new ValidityResponse(request.BaseDate, snapshot.ServerTime, FormatUtc(canonical), valid);
    }

    private DateTimeOffset Expiry(DateTime day) => new(TimeZoneInfo.ConvertTimeToUtc(
        DateTime.SpecifyKind(day.AddDays(1), DateTimeKind.Unspecified), timeZone));

    internal static DateTime ParsePersianDate(string text)
    {
        if (text is null || !Regex.IsMatch(text, @"\A[0-9]{4}/[0-9]{2}/[0-9]{2}\z"))
            throw new ArgumentException("Invalid Persian date.", nameof(text));
        var parts = text.Split('/').Select(x => int.Parse(x, CultureInfo.InvariantCulture)).ToArray();
        return new PersianCalendar().ToDateTime(parts[0], parts[1], parts[2], 0, 0, 0, 0);
    }
    internal static string FormatPersianDate(DateTime day)
    {
        var calendar = new PersianCalendar();
        return string.Create(CultureInfo.InvariantCulture, $"{calendar.GetYear(day):D4}/{calendar.GetMonth(day):D2}/{calendar.GetDayOfMonth(day):D2}");
    }
    internal static DateTimeOffset ParseUtc(string text)
    {
        if (text is null || !Regex.IsMatch(text, @"^[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}(\.[0-9]{1,7})?Z\z") ||
            !DateTimeOffset.TryParseExact(text, ["yyyy-MM-dd'T'HH:mm:ss'Z'", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'"],
                CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var result))
            throw new ArgumentException("Invalid UTC timestamp.", nameof(text));
        return result;
    }
    internal static string FormatUtc(DateTimeOffset utc) => utc.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'", CultureInfo.InvariantCulture);
}
