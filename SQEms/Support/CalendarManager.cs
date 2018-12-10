using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Support
{
    public static class CalendarManager
    {
        /// <summary>
        /// Gets the date of the sunday of the given week.
        /// </summary>
        /// <param name="month">The month.</param>
        /// <param name="day">The day.</param>
        /// <returns>The day of the sunday.</returns>
        public static int GetSundayOfWeek(int month, int day)
        {
            DateTime now = new DateTime(ConvertMonthToYear(ref month), month, day);

            while(now.DayOfWeek != DayOfWeek.Sunday)
            {
                now.AddDays(-1);
            }

            return now.Day;
        }

        /// <summary>
        /// Calculates out the year, if the month is an integer since 1970, January.
        /// Also wraps the month to 0-11.
        /// </summary>
        /// <param name="month">The month</param>
        /// <returns>The year.</returns>
        public static int ConvertMonthToYear(ref int month)
        {
            int yearoffset = month / 12;

            month %= 12;

            return yearoffset + 1970;
        }

        /// <summary>
        /// Converts a year and a month to a absolute month.
        /// </summary>
        /// <param name="year">The year.</param>
        /// <param name="month">The month.</param>
        /// <returns>The absolute month.</returns>
        public static int ConvertYearMonthToMonth(int year, int month)
        {
            return (year - 1970) * 12 + month;
        }

        /// <summary>
        /// Gets the short month name for a given month.
        /// </summary>
        /// <param name="month">The month.</param>
        /// <returns>The name.</returns>
        public static string GetMonthName(int month)
        {
            return new DateTime(ConvertMonthToYear(ref month), month, 1).ToString("MMM", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Gets 'today'.
        /// </summary>
        /// <returns>The current date.</returns>
        public static DateTime GetToday()
        {
            return new DateTime(2017, 11, 15);
        }

        /// <summary>
        /// Normalizes a date to the sunday of the week.
        /// </summary>
        /// <param name="date">The date.</param>
        public static void NormalizeDate(ref DateTime date, DayOfWeek startOfWeek = DayOfWeek.Sunday)
        {
            int diff = (7 + (date.DayOfWeek - startOfWeek)) % 7;
            date = date.AddDays(-1 * diff).Date;
        }

    }
}
