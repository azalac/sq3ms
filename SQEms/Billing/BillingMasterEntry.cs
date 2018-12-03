/*
* FILE          : BillingMasterEntry.cs
* PROJECT       : INFO-2180 Software Quality 1, Term Project
* PROGRAMMER    : Blake Ribble
* FIRST VERSION : November 1, 2018
*/

using Support;
using System;
using System.IO;

namespace Billing
{
    /// <summary>
    /// Public class which represents the master entry for a billing code description.
    /// </summary>
    public class BillingMasterEntry
    {
        /// <summary>
        /// The fee code for this billing description.
        /// </summary>
        public string FeeCode { get; private set; }

        /// <summary>
        /// The effective starting date for the billing description.
        /// </summary>
        public string EffectiveDate { get; private set; }

        /// <summary>
        /// The amount the procedure costs.
        /// </summary>
        public string DollarAmount { get; private set; }

        /// <summary>
        /// Reads the master description file into the database.
        /// </summary>
        /// <param name="data">The master file data</param>
        /// <param name="database">The database to insert into</param>
        /// <remarks>
        /// 
        /// Calls <see cref="ParseFromString(string)"/>.
        /// 
        /// Pass each line to the method. If the method returns a null, 
        /// log the error and ignore the line.
        /// 
        /// </remarks>
        /// 

        public static void Initialize(string data, DatabaseManager database)
        {

            //Create a new instance of the logging class so error could be logged
            Logging logger = new Logging();

            // this table is not yet implemented
            DatabaseTable BillingDescription = database["BillingMaster"];

            //Create a string[] reading all data from text file
            string[] masterBillingFiles = { };

            try
            {
                //Reads all lines from the master file
               masterBillingFiles = data.Split('\n');
            }

            catch(Exception)
            {
                //Log the error
                logger.Log(Definitions.LoggingInfo.ErrorLevel.ERROR, "Master file cannot be found");
            }
           

            //For each billing code in master file
            foreach(string code_ in masterBillingFiles)
            {
                // Remove any extra whitespace
                string code = code_.Trim();

                // Ignore the line if it's empty, or if it starts with two dashes
                if(code.Length == 0 || code.StartsWith("--"))
                {
                    continue;
                }

                //Parse the information from the line
                BillingMasterEntry billingEntry = ParseFromString(code);
                
                //If there was a length error
                if(billingEntry == null)
                {
                    //Log the error, and continue with code
                    logger.Log(Definitions.LoggingInfo.ErrorLevel.ERROR, "Incorrect length of billing code");

                    continue;
                }
                else
                {
                    //Insert the information into the table
                    BillingDescription.Insert(billingEntry.FeeCode, billingEntry.EffectiveDate, billingEntry.DollarAmount);
                }
            }
        }

        /// <summary>
        /// Parses a string into a Billing Entry.
        /// </summary>
        /// <remarks>
        /// 
        /// The first 4 characters are the fee code.
        /// The next 8 characters are the effective date (YYYYMMDD).
        /// The last 11 characters are the dollar amount (7.2 as float format).
        /// 
        /// </remarks>
        /// <param name="str">The string</param>
        /// <returns>The parsed BillingMasterEntry</returns>
        public static BillingMasterEntry ParseFromString(string data)
        {
            //Create a new instance of the parsedCode
            BillingMasterEntry parsedCode = new BillingMasterEntry();

            //Variable that will hold the temporary dollar amount for parsing
            string tempAmount;

            //If the length is less than 23, not a proper billing code length
            if(ValidMasterEntry(data))
            {
                
                //Set the fee code
                parsedCode.FeeCode = data.Substring(0, 4);

                //Set the effective date
                parsedCode.EffectiveDate = data.Substring(4, 8);
                
                //Set the temp dollar amount into temp variable
                tempAmount = data.Substring(12, 11);
                
                //Get rid of the leading zeroes
                tempAmount = tempAmount.TrimStart('0');

                //Get the length of the string to get rid of the two ending zeroes
                int amountLength = tempAmount.Length - 2;

                //Remove those zeroes
                tempAmount = tempAmount.Remove(amountLength, 2);

                //Set the dollar amount to properly formatted amount
                parsedCode.DollarAmount = tempAmount.Substring(0, amountLength - 2) + "." + tempAmount.Substring(amountLength - 2);

                //Return the parsed BillingMasterEntry

                return parsedCode;
            }

            else
            {   
                //Length error
                return null;         
            }   
        }
        
        /// <summary>
        /// Checks if a provided string is a valid master entry.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <returns><code>true</code> if the string is valid</returns>
        private static bool ValidMasterEntry(string str)
        {
            string pattern = @"^(?'code'\w\d\d\d)(?'year'\d{4})(?'month'[01][0-9])(?'day'[0-3][0-9])(?'amt1'\d{7})(?'amt2'\d{4})$";

            return System.Text.RegularExpressions.Regex.IsMatch(str, pattern);
        }
    }
}
