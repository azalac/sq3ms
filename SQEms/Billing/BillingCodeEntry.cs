/*
* FILE          : BillingCodeEntry.cs
* PROJECT       : INFO-2180 Software Quality 1, Term Project
* PROGRAMMER    : Blake Ribble and Austin Zalac
* FIRST VERSION : November 1, 2018
*/

using Demographics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Definitions;
using Support;
using System.IO;    

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
        //Definitions of class level variables
        private static DatabaseTable billing;
        private static DatabaseTable billingMaster;

        /// <summary>
        /// Constructor that initializes class level variables
        /// </summary>
        /// <param name="database"> Used to obtain the information from database</param>
        
        public BillingFileInteraction(DatabaseManager database)
        {
            billing = database["Billing"];
            billingMaster = database["BillingMaster"];        
        }

        /// <summary>
        /// Writes the billing information to the file at the given path.
        /// </summary>
        /// <param name="database"> Used to obtain the information for billing code</param>
        /// <param name="path"> Path of the billing code</param>

        public static void CreateBillingCode(DatabaseManager database, string path, string feeCode, object appID)
        {
            int ID = (int)appID;

            //Gets the current date of service
            string date = GetDateOfService();

            //Create a new instance of the log class - used for errors
            Logging logger = new Logging();
            
            //Set database table variables with information 
            DatabaseTable Master = database["BillingMaster"];
            DatabaseTable testBilling = database["Billing"];

            //Set an object variable that holds the primary key for dollar amount
            object name = null;

            try
            {
                //Look in the Masterfile database for the feeCode being searched for
                name = Master.WhereEquals<string>("BillingCode", feeCode).First();
            }

            //If the feeCode doesn't exist
            catch(InvalidOperationException)
            {
                logger.Log(LoggingInfo.ErrorLevel.ERROR, "Billing Code was not found in database");
                return;
            }
            
            //Convert the name of the object to a string
            string BillName = name.ToString();

            //Use the feeCode found and search for the fee amount in the DollarAmount coloumn
            object cost = Master[BillName, "DollarAmount"];

            //Convert the cost of the object to a string
            string costWithDecimal = cost.ToString();

            string[] numSplit = costWithDecimal.Split('.');

            string finalCost = numSplit[0] + numSplit[1];


            //Create an instance of the patients table to obtain health card with patient ID
            DatabaseTable patientInfo = database["Patients"];

            //Store the info in an object
            object info = patientInfo[ID, "HCN"];

            //Convert object to string
            string patientHCN = info.ToString();

            //Create the billing code to send to text file - need to left pad final code and fee code
            string billingFile = date + patientHCN + feeCode + finalCost + "00";

            //Get the length of the billing file
            int length = billingFile.Length;

            //Length is 36 characters long for file, subtract what we have, the remaining number is how many zeroes needed for left padding

            Console.WriteLine(billingFile);

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

        public static string GetDateOfService()
        {
            DateTime theDate = DateTime.Now;
            string strToday = theDate.ToString("yyyyMMdd");
            return strToday;
        }

    }
    
}
