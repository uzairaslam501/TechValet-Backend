using System.Globalization;
using System.Reflection.Metadata;
using System;
using NodaTime;
using NodaTime.Extensions;
using GeoTimeZone;
using TimeZoneConverter;

namespace ITValet.HelpingClasses
{
    public class DateTimeHelper
    {
        // TODO Get FirstDayOfWeek from request culture instead from machine culture once have localization added
        public static DateTime StartOfWeek =>
            DateTime.SpecifyKind(DateTime.Today.AddDays((int)CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek - (int)DateTime.Today.DayOfWeek), DateTimeKind.Utc);

        public static DateTime StartOfNextWeek => StartOfWeek.AddDays(7);

        public static DateTime StartOfMonth => new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        public static DateTime StartOfNextMonth => StartOfMonth.AddMonths(1);

        public static DateTime StartOfLastMonth => StartOfMonth.AddMonths(-1);

        public static DateTime EndOfNextMonth => StartOfNextMonth.AddMonths(1);

        public static DateTime StartOfYear => new DateTime(DateTime.UtcNow.Year, 1, 1);

        public static DateTime EndOfYear => new DateTime(DateTime.UtcNow.Year, 12, 31, 23, 59, 59);

        public static string CalculateTimeZoneIanaId(double latitude, double longitude)
        {
            var timeZoneIanaResult = TimeZoneLookup.GetTimeZone(latitude, longitude);
            return timeZoneIanaResult.Result;
        }

        public static string CalculateTimeZoneWindowsId(double latitude, double longitude)
        {
            var timeZoneIanaResult = TimeZoneLookup.GetTimeZone(latitude, longitude);
            return TZConvert.IanaToWindows(timeZoneIanaResult.Result);
        }

        public static IEnumerable<string> GetAllIanaTimeZoneNames()
        {
            return DateTimeZoneProviders.Tzdb.GetAllZones().Select(t => t.Id);
        }

        public static DateTime GetZonedDateTimeFromUtc(DateTime utcDateTime, string ianaTimeZoneId)
        {
            var timeZone = DateTimeZoneProviders.Tzdb[ianaTimeZoneId];
            Instant instant = Instant.FromDateTimeUtc(DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc));
            return instant.InZone(timeZone).ToDateTimeUnspecified();
        }

        public static DateTime GetUtcTimeFromZoned(DateTime dateTime, string ianaTimeZoneId)
        {
            var dateTimeZone = DateTimeZoneProviders.Tzdb[ianaTimeZoneId];
            var localDateTime = LocalDateTime.FromDateTime(dateTime);
            var zonedDateTime = dateTimeZone.AtStrictly(localDateTime);

            return zonedDateTime.ToInstant().ToDateTimeUtc();
        }

        public static bool TryGetUtcTimeFromZoned(DateTime dateTime, string ianaTimeZoneId, out DateTime result)
        {
            try
            {
                var dateTimeZone = DateTimeZoneProviders.Tzdb[ianaTimeZoneId];
                var localDateTime = LocalDateTime.FromDateTime(dateTime);
                var zonedDateTime = dateTimeZone.AtStrictly(localDateTime);

                result = zonedDateTime.ToInstant().ToDateTimeUtc();
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                result = default;
                return false;
            }
        }

        public static readonly Dictionary<string, string> TimeZoneFriendlyNames = new Dictionary<string, string>
        {
            { "Canada/Atlantic", "Canada/Atlantic" },
            { "Canada/Central", "Canada/Central" },
            { "Canada/Eastern", "Canada/Eastern" },
            { "Canada/Mountain", "Canada/Mountain" },
            { "Canada/Newfoundland", "Canada/Newfoundland" },
            { "Canada/Pacific", "Canada/Pacific" },
            { "Canada/Saskatchewan", "Canada/Saskatchewan" },
            { "Canada/Yukon", "Canada/Yukon" } 
        };
    }
}
