/*
* FILE          : BillingCodeEntry.cs
* PROJECT       : INFO-2180 Software Quality 1, Term Project
* PROGRAMMER    : Blake Ribble and Austin Zalac
* FIRST VERSION : November 23, 2018
*/

using System;
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
        /// <param name="feeCode"> The fee code generated depending on the encounter</param>
        /// <param name="appID"> The appointment ID</param>

        public void CreateBillingCode(DatabaseManager database, string feeCode, object appID)
        {

            //Create a new instance of the log class - used for errors
            Logging logger = new Logging();

            //Get the appointment ID passed in
            int ID;

            //Try and convert the object to an int(if not an int already) - > if errors, please change
            try
            {
                ID = (int)appID;
            }
            catch (Exception)
            {

                //Log the error
                logger.Log(LoggingInfo.ErrorLevel.ERROR, "Could not successfully parse the appointment ID");
                return;
            }

            //Gets the current date of service
            string date = GetDateOfService();

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
            catch (InvalidOperationException)
            {
                //Log the error
                logger.Log(LoggingInfo.ErrorLevel.ERROR, "Billing Code was not found in database");
                return;
            }

            //Convert the name of the object to a string
            string BillName = name.ToString();

            //Use the feeCode found and search for the fee amount in the DollarAmount coloumn
            object cost = Master[BillName, "DollarAmount"];

            //Convert the cost of the object to a string
            string costWithDecimal = cost.ToString();

            //Create a string array to split the cost to get rid of decimal
            string[] numSplit = costWithDecimal.Split('.');

            //Combine the left side and right side of cost
            string finalCost = numSplit[0] + numSplit[1];

            //Create an instance of the patients table to obtain health card with patient ID
            DatabaseTable patientInfo = database["Patients"];

            //Store the info in an object
            object info = patientInfo[ID, "HCN"];

            //Convert object to string
            string patientHCN = info.ToString();

            //Store the sex info in an object
            object patientSex = patientInfo[ID, "sex"];

            //Conver the object to a string
            string sex = patientSex.ToString();

            //Create the billing code to send to text file - need to left pad final code and fee code
            string tempBillingCode = date + patientHCN + sex + feeCode + finalCost + "00";

            //Get the length of the billing file
            int length = tempBillingCode.Length;

            //A string to hold the number of 0's needed to pad the cost
            string zeroPadded = "";

            //Loop through and get the number of zeroes needed to pad cost (if number is huge, not a lot of zeroes, etc)
            for (int padLength = length; padLength < 36; padLength++)
            {
                zeroPadded = zeroPadded + "0";
            }

            //Generate the final response code to send to the ministry
            string finalResponse = date + patientHCN + sex + feeCode + zeroPadded + finalCost + "00";

            //Write information to billing codes file
            WriteInfoToFile("../../BillingCodes.txt", finalResponse);
        }

        /// <summary>
        /// Writes the billing information to the file at the given path.
        /// </summary>
        /// <param name="path"> Path to write the constructed billing code to </param>
        /// <param name="info"> The billing code</param>
        /// 
        private void WriteInfoToFile(string path, string info)
        {
            //Write to the file specified
            using (StreamWriter sw = File.AppendText(path))
            {
                sw.WriteLine(info);
            }
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
        public void WriteStatistics(string path, int month)
        {

            //Variables that hold information needed for report
            int totalEncounters = 0;
            int paidEncounters = 0;
            int toFollowEncounters = 0;
            float billedProcedures = 0;
            float receivedTotal = 0;
            float receivedPercentage = 0;
            float averageBilling = 0;
            
            //Loop through each billing code where the month is the month specified
            foreach(object key in billing.WhereEquals("Month", month))
            {
                totalEncounters++;

                //Convert the value
                float.TryParse(billing[key, "Fee"].ToString(), out float billed);
                billedProcedures += billed;

                //If the result is paid
                if ((BillingCodeResponse)billing[key, "CodeResponse"] == BillingCodeResponse.PAID)
                {
                    paidEncounters++;
                    receivedTotal += billed;
                }
            }

            //Calculate the number of follow up encounters needed
            toFollowEncounters = totalEncounters - paidEncounters;

            //Calculate the percentage of recieved profits
            receivedPercentage = (receivedTotal / totalEncounters) * 100;

            //Calculate the average billing amount
            averageBilling = receivedTotal / totalEncounters;

            //Build the report
            StringBuilder saveToFile = new StringBuilder();
            saveToFile.AppendFormat("Total Encounters Billed: {0}\n" +
                                    "Total Billed Procedures: {1}\n" +
                                    "Received Total: {2}\n" +
                                    "Received Percentage: {3}\n" +
                                    "Average Billing: {4}\n" +
                                    "Encounters To Follow-up: {5}\n", 
                                    totalEncounters, billedProcedures, receivedTotal, receivedPercentage, averageBilling, toFollowEncounters);

            //Write the information to a file
            WriteInfoToFile("../../MonthlyReport.txt", saveToFile.ToString());
        }

        /// <summary>
        /// Gets the current date in a specified format
        /// </summary>
        /// <returns>string strToday</returns>
        public static string GetDateOfService()
        {
            DateTime theDate = DateTime.Now;
            string strToday = theDate.ToString("yyyyMMdd");
            return strToday;
        }

        /// <summary>
        /// Parse the response code lines in file provided
        /// </summary>
        /// <param name="path"> Path which contains response codes </param>
        /// 

        public void ParseResponseFile(string path)
        {
            //Create a new instance of the log class - used for errors
            Logging logger = new Logging();

            //Store all of the lines from response file in a string array
            string[] responseLines = File.ReadAllLines(path);

            //For each response code
            foreach(string respCode in responseLines)
            {
                //If the length of the code is 40 characters (if not 40 characters, skip the line)
                if(respCode.Length == 40)
                {
                    //Integer variables that represent the date
                    int intYear = 0;
                    int intMonth = 0;
                    int intDay = 0;

                    //Create string variables that hold parsed information from response string
                    string yearString = respCode.Substring(0, 4);
                    string monthString = respCode.Substring(4, 2);
                    string dayString = respCode.Substring(6, 2);
                    string hcn = respCode.Substring(8, 12);
                    string paymentStatus = respCode.Substring((respCode.Length - 4), 4);

                    //Try and convert string to int
                    try
                    {
                        //Convert each date string into an int
                        intYear = int.Parse(yearString);
                        intMonth = int.Parse(monthString);
                        intDay = int.Parse(dayString);
                    }

                    //If an exception was thrown
                    catch (Exception)
                    {
                        logger.Log(LoggingInfo.ErrorLevel.ERROR, "Error converting info in response file");
                    }

                    //ADD LOGIC FOR CHANGING THE PAYMENT STATUS TO RESPONSE FILE STATUS IN BILLING
                }              
            }
        }
    }   
}
