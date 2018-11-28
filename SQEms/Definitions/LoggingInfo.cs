/*
* FILE          : Logging.cs
* PROJECT       : INFO-2180 Software Quality 1, Term Project
* PROGRAMMER    : Billy Parmenter
* FIRST VERSION : November 1, 2018
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Definitions
{
    /**
      * NAME    : LoggingInfo
      * PURPOSE : The LoggingInfo class holds data members used in the LoggingClass
      */
    public class LoggingInfo
    {
        /// <summary>
        /// The file path for where to save the logs
        /// </summary>
        public const string logFilePath = @".\LogFile.txt";


        /// <summary>
        /// The error levels of the logs
        /// </summary>
        public enum ErrorLevel
        {
            OFF,
            DEBUG,
            INFO,
            WARN,
            ERROR,
            FATAL,
            ALL
        }
    }
}

