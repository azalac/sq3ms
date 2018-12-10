/*
* FILE          : DebugLog.cs
* PROJECT       : INFO-2180 Software Quality 1, Term Project
* PROGRAMMER    : Austin Zalac
* FIRST VERSION : November 12, 2018
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulingUI
{
    class DebugLog
    {
        public const int COMPONENT_EVENTS = 0b1,
            CONTROLLER_EVENTS = 0b10,
            ETC_EVENTS = 0b100;

        private static int Level = CONTROLLER_EVENTS | ETC_EVENTS;

        /// <summary>
        /// Logs the message to Debug
        /// </summary>
        /// <param name="level">The level of message</param>
        /// <param name="message">The log message</param>
        public static void Log(int level, object message)
        {
            if((level & Level) != 0)
            {
                System.Diagnostics.Debug.WriteLine(message);
            }
        }

        /// <summary>
        /// Logs component events
        /// </summary>
        /// <param name="message">The log message</param>
        public static void LogComponent(object message)
        {
            Log(COMPONENT_EVENTS, message);
        }

        /// <summary>
        /// Logs controller events
        /// </summary>
        /// <param name="message">The log message</param>

        public static void LogController(object message)
        {
            Log(CONTROLLER_EVENTS, message);
        }

        /// <summary>
        /// Logs ETC events
        /// </summary>
        /// <param name="message">The log message</param>
        public static void LogOther(object message)
        {
            Log(ETC_EVENTS, message);
        }

    }
}
