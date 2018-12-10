/*
* FILE          : CalendarInfo.cs - Definitions
* PROJECT       : INFO-2180 Software Quality 1, Term Project
* PROGRAMMER    : Austin Zalac
* FIRST VERSION : November 20, 2018
*/
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

