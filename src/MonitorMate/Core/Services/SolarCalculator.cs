namespace MonitorMate.Core.Services;

public sealed record SolarTimes(DateTime Sunrise, DateTime Sunset, bool IsPolarFallback = false);

public static class SolarCalculator
{
    public static SolarTimes? Calculate(DateOnly date, double latitude, double longitude)
    {
        if (double.IsNaN(latitude) || double.IsNaN(longitude) || Math.Abs(latitude) > 90 || Math.Abs(longitude) > 180)
        {
            return null;
        }

        var tz = TimeZoneInfo.Local;
        var sunriseUtc = CalculateEvent(date, latitude, longitude, isSunrise: true);
        var sunsetUtc = CalculateEvent(date, latitude, longitude, isSunrise: false);
        if (sunriseUtc is null || sunsetUtc is null)
        {
            return null;
        }

        try
        {
            var sunrise = ResolveLocalDate(sunriseUtc.Value, date, tz);
            var sunset = ResolveLocalDate(sunsetUtc.Value, date, tz);
            return sunrise is null || sunset is null ? null : new SolarTimes(sunrise.Value, sunset.Value);
        }
        catch
        {
            return null;
        }
    }

    private static DateTime? ResolveLocalDate(DateTime utcEvent, DateOnly date, TimeZoneInfo zone)
    {
        // The astronomical calculation normalizes UTC hours, losing the UTC-day offset.
        for (var offset = -1; offset <= 1; offset++)
        {
            var local = TimeZoneInfo.ConvertTimeFromUtc(utcEvent.AddDays(offset), zone);
            if (DateOnly.FromDateTime(local) == date) return local;
        }
        return null;
    }

    private static DateTime? CalculateEvent(DateOnly date, double latitude, double longitude, bool isSunrise)
    {
        var dayOfYear = date.DayOfYear;
        var lngHour = longitude / 15.0;
        var t = dayOfYear + ((isSunrise ? 6 : 18) - lngHour) / 24.0;
        var m = 0.9856 * t - 3.289;
        var l = m + 1.916 * Sin(m) + 0.020 * Sin(2 * m) + 282.634;
        l = NormalizeDegrees(l);

        var ra = RadToDeg(Math.Atan(0.91764 * Math.Tan(DegToRad(l))));
        ra = NormalizeDegrees(ra);
        var lQuadrant = Math.Floor(l / 90) * 90;
        var raQuadrant = Math.Floor(ra / 90) * 90;
        ra += lQuadrant - raQuadrant;
        ra /= 15;

        var sinDec = 0.39782 * Sin(l);
        var cosDec = Math.Cos(Math.Asin(sinDec));
        var cosH = (Math.Cos(DegToRad(90.833)) - sinDec * Sin(latitude)) / (cosDec * Cos(latitude));
        if (cosH > 1 || cosH < -1)
        {
            return null;
        }

        var h = isSunrise ? 360 - RadToDeg(Math.Acos(cosH)) : RadToDeg(Math.Acos(cosH));
        h /= 15;
        var localMeanTime = h + ra - 0.06571 * t - 6.622;
        var utcHours = NormalizeHours(localMeanTime - lngHour);

        var baseUtc = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        return baseUtc.AddHours(utcHours);
    }

    private static double Sin(double degrees) => Math.Sin(DegToRad(degrees));
    private static double Cos(double degrees) => Math.Cos(DegToRad(degrees));
    private static double DegToRad(double degrees) => degrees * Math.PI / 180.0;
    private static double RadToDeg(double radians) => radians * 180.0 / Math.PI;

    private static double NormalizeDegrees(double value)
    {
        value %= 360;
        return value < 0 ? value + 360 : value;
    }

    private static double NormalizeHours(double value)
    {
        value %= 24;
        return value < 0 ? value + 24 : value;
    }
}
