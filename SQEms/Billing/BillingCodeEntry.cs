using Demographics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Definitions;
using Support;

namespace Billing
{
    /// <summary>
    /// Reads & Writes MOH billing files and generates monthly summaries.
    /// </summary>
    /// <remarks>
    /// 
    /// Entry format:
    /// 
    /// The first 8 characters are the DateOfService (YYYYMMDD).
    /// The next 12 characters are the patient's HCN (10 numeric, 2 alphabetic).
    /// The next 1 character is the patient's sex (M/F/I/H, <see cref="SexTypes"/>).
    /// The next 11 characters are the Fee (7 integers, 4 decimals, no period, zero padded).
    /// 
    /// </remarks>
    public class BillingFileInteraction
    {

        private readonly DatabaseTable billing;
        
        public BillingFileInteraction(DatabaseManager database)
        {
            billing = database["Billing"];
        }
        
        /// <summary>
        /// Parses the file at the given path into the database.
        /// </summary>
        /// <param name="path"></param>
        public void Parse(string path)
        {


        }
        
        /// <summary>
        /// Writes the billing information to the file at the given path.
        /// </summary>
        /// <param name="path"></param>
        public void Write(string path)
        {

        }

        /// <summary>
        /// Generates a monthly billing summary file.
        /// </summary>
        /// <param name="path">The path to output to</param>
        /// <remarks>
        /// Fields:
        /// 
        /// Total Encounters Billed (count)
        /// Total Billed Procedures (dollars)
        /// Received Total (dollars)
        /// Received Percentage (RT/TBP * 100%)
        /// Average Billing (RT / TEB in dollars)
        /// Num. Encounters to Follow-up (count of FHCV and CMOH)
        /// 
        /// Format is supposed to be CSV, but no layout specified?
        /// </remarks>
        public void WriteStatistics(string path)
        {

        }

    }
    
}
