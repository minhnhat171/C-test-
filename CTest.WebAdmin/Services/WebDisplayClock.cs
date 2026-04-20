namespace CTest.WebAdmin.Services;

public sealed class WebDisplayClock
{
    private static readonly string[] PreferredTimeZoneIds =
    [
        "SE Asia Standard Time",
        "Asia/Bangkok"
    ];

    public TimeZoneInfo TimeZone { get; } = ResolveTimeZone();

    public DateTimeOffset Now => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZone);

    public DateTimeOffset ToDisplayTime(DateTimeOffset utcValue)
    {
        var normalizedUtc = utcValue.Offset == TimeSpan.Zero
            ? utcValue
            : utcValue.ToUniversalTime();

        return TimeZoneInfo.ConvertTime(normalizedUtc, TimeZone);
    }

    private static TimeZoneInfo ResolveTimeZone()
    {
        foreach (var timeZoneId in PreferredTimeZoneIds)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Local;
    }
}
