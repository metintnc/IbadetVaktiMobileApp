using System.Globalization;

namespace hadis.Helpers;

internal static class PrayerCalendarDate
{
    internal static bool TryParse(string? value, int year, int month, out DateTime date) =>
        DateTime.TryParseExact(value, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
        && date.Year == year && date.Month == month;

    // Older calendar files may have a transposed ISO date. AlAdhan's readable date
    // was saved verbatim, so the original day can still be recovered offline.
    internal static bool TryReadOriginalDate(string? readable, out DateTime date) =>
        DateTime.TryParseExact(readable, "d MMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
}
