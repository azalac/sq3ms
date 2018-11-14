using System;

namespace Definitions
{
	public static class CalendarInfo
	{
        public static readonly int[] MAX_APPOINTMENTS = new int[]{
            2,
            6,
            6,
            6,
            6,
            6,
            2
        };

        /// <summary>
        /// The maximum appointments that will ever be scheduled on any day.
        /// </summary>
        public const int MAX_APPOINTMENTS2 = 6;

        public const int WEEK_LENGTH = 7;
	}
}

