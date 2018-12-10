/*
* FILE          : FileIO.cs
* PROJECT       : INFO-2180 Software Quality 1, Term Project
* PROGRAMMER    : Billy Parmenter
* FIRST VERSION : November 23, 2018
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Support
{
    public delegate string ProdecureGenerator(int aptid, int procid);

    public static class FileIO
    {
        /// <summary>
        /// Method that writes all billable procedures
        /// <param name="path"> Path being written to</param>
        /// <param name="appointment"> Appointment database table </param>
        /// <param name="procedures"> Procedure database table </param>
        /// <param name="month"> Month being searched for</param>
        /// <param name="generator"> Generator for procedure</param>
        /// </summary>
        /// 
        public static void WriteAllBillableProcedures(string path,
            DatabaseTable appointment, DatabaseTable procedures,
            int month, ProdecureGenerator generator)
        {
            StringBuilder lines = new StringBuilder();

            foreach(object apt_pk in appointment.WhereEquals("Month", month))
            {
                foreach(object procedure_pk in procedures.WhereEquals("AppointmentID", apt_pk))
                {
                    lines.AppendLine(generator((int)apt_pk, (int)procedure_pk));
                }
            }

            File.WriteAllText(path, lines.ToString());
        }

        /// <summary>
        /// Writes the monthly summary
        /// <param name="path"> Path being written to</param>
        /// </summary>
        /// 
        public static void WriteMonthlySummary(string path)
        {

        }

        /// <summary>
        /// Writes the monthly summary
        /// <param name="path"> Path being written to</param>
        /// <returns>string[] of data</returns>
        /// </summary>
        /// 
        public static string[] GetResponseFileData(string path)
        {
            return File.Exists(path) ? File.ReadAllLines(path) : null;
        }
        
    }
}
