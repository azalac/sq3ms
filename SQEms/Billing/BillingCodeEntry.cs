/*
* FILE          : BillingCodeEntry.cs
* PROJECT       : INFO-2180 Software Quality 1, Term Project
* PROGRAMMER    : Blake Ribble 
* FIRST VERSION : November 23, 2018
*/

using System;
using System.Linq;
using System.Text;
using Definitions;
using Support;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Billing
{
    /// <summary>
    /// Struct that contains billable procedure information and methods
    /// </summary>
    /// 
    struct BillableProcedure
    {
        public int year, month, day;
        public string HCN;
        public char sex;
        public string code;
        public string fee;
        public string response;

        /// <summary>
        /// Method which sets variables if obj is BillableProcedure
        /// </summary>
        /// <param name="obj">The object</param>
        /// <returns> false if object is not part of billable procedure, values if it is part of billable procedure </returns>
        /// 
        public override bool Equals(object obj)
        {
            //If obj is not billableprocedure
            if (!(obj is BillableProcedure))
            {
                return false;
            }

            //Fill information and return it
            var procedure = (BillableProcedure)obj;
            return year == procedure.year &&
                   month == procedure.month &&
                   day == procedure.day &&
                   HCN == procedure.HCN &&
                   sex == procedure.sex &&
                   code == procedure.code;
        }

        /// <summary>
        /// Gets the hashcode
        /// </summary>
        /// <returns>hashCode</returns>
        /// 
        public override int GetHashCode()
        {
            var hashCode = 1805857402;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + year.GetHashCode();
            hashCode = hashCode * -1521134295 + month.GetHashCode();
            hashCode = hashCode * -1521134295 + day.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(HCN);
            hashCode = hashCode * -1521134295 + sex.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(code);
            return hashCode;
        }

        /// <summary>
        /// Converts variable to specified string format
        /// </summary>
        /// <returns>Formatted string </returns>
        /// 
        public override string ToString()
        {
            return string.Format("{0}, {1}, {2}; {3}:{4}; {5}:{6}", year, month, day, HCN, sex, code, response);
        }
    }

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
    /// The next 4 characters are the billing code.
    /// The next 11 characters are the Fee (7 integers, 4 decimals, no period, zero padded).
    /// 
    /// </remarks>
    public class BillingFileInteraction
    {
        //Regex definition
        private const string BP_REGEX = 
            @"^(?'year'\d{4})(?'month'\d{2})(?'day'\d{2})(?'hcn'\d{10}\w{2})(?'sex'M|F|I|H)(?'code'\w\d{3})(?'amt'\d{11})(?'resp'PAID|DECL|FHCV|CMOH)?$";

        //Definitions of class level variables
        private DatabaseTable procedures;
        private DatabaseTable billingMaster;
        private DatabaseTable people;
        private DatabaseTable appointments;

        /// <summary>
        /// Constructor that initializes class level variables
        /// </summary>
        /// <param name="database"> Used to obtain the information from database</param>
        
        public BillingFileInteraction(DatabaseManager database)
        {
            procedures = database["Billing"];
            billingMaster = database["BillingMaster"];

            people = database["Patients"];
            appointments = database["Appointments"];
        }
        
        /// <summary>
        /// Generates a billable procedure line.
        /// </summary>
        /// <remarks>
        /// 
        /// Format:
        /// 
        /// 20171120 1234567890KV F   A665 00000913500
        /// YYYYMMDD HCN          Sex Code Price
        /// 
        /// Note: there are no spaces in the actual line
        /// 
        /// </remarks>
        /// <param name="appointment"> The appointment ID</param>
        /// <param name="procedure"> The proceduer ID</param>
        /// <returns>Full billing code</returns>
        public string GenerateBillableProcedureLine(int appointment, int procedure)
        {
            //Get the date information
            int month = (int)appointments[appointment, "Month"];
            DateTime date = new DateTime(CalendarManager.ConvertMonthToYear(ref month), month, (int)appointments[appointment, "Day"]);

            //Get the patient pk and store in object
            object patient_pk = appointments[appointment, "PatientID"];

            //Gets the HCN at corresponding pk
            string HCN = (string)people[patient_pk, "HCN"];

            //Gets the sex at corresponding pk
            SexTypes sex = (SexTypes)people[patient_pk, "sex"];

            //Gets the fee code at corresponding pk
            string code = (string)procedures[procedure, "BillingCode"];

            //Gets the fee price at corresponding pk
            string price = (string)billingMaster[code, "DollarAmount"];

            return date.ToString("yyyyMMdd") + HCN + sex.ToString() + code + price;
        }

        /// <summary>
        /// Method that generates the monthly billing files
        /// </summary>
        /// <param name="month"> The month being generated</param>
        /// <param name="path">The path being written to</param>
        public void GenerateMonthlyBillingFile(int month, string path)
        {
            FileIO.WriteAllBillableProcedures(path, appointments, procedures, month, GenerateBillableProcedureLine);
        }

        /// <summary>
        /// Generates the contents for a monthly billing summary.
        /// </summary>
        /// <param name="month">The month to compile from</param>
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
        /// </remarks>
        public string CompileStatistics(int month)
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
            foreach(object key in procedures.Keys.Where(pk => ((int)appointments[procedures[pk, "AppointmentID"], "Month"] == month)))
            {
                totalEncounters++;

                //Convert the value
                float.TryParse(billingMaster[procedures[key, "BillingCode"], "DollarAmount"].ToString(), out float billed);
                billedProcedures += billed;

                //If the result is paid
                if ((BillingCodeResponse)procedures[key, "CodeResponse"] == BillingCodeResponse.PAID)
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
            StringBuilder data = new StringBuilder();
            data.AppendFormat("Total Encounters Billed: {0}\n" +
                                    "Total Billed Procedures: {1}\n" +
                                    "Received Total: {2}\n" +
                                    "Received Percentage: {3}\n" +
                                    "Average Billing: {4}\n" +
                                    "Encounters To Follow-up: {5}\n", 
                                    totalEncounters, billedProcedures, receivedTotal, receivedPercentage, averageBilling, toFollowEncounters);
            return data.ToString();
        }
        
        /// <summary>
        /// Parse the response code lines in file provided
        /// </summary>
        /// <param name="month"> The month being searched </param>
        /// <param name="path"> The path being written to </param>
        /// <returns>False if file is empty, true is successful</returns>
        ///
        public bool ParseResponse(int month, string path)
        {
            //Create a new instance of the log class - used for errors
            Logging logger = new Logging();

            //Get all of the lines from response file and store in string array
            string[] data = FileIO.GetResponseFileData(path);

            //If the string array is null, return false
            if(data == null)
            {
                return false;
            }

            //Call method to match procedures
            MatchProcedures(month, ParseData(data, logger), logger);

            return true;
        }

        /// <summary>
        /// Matches the procedures
        /// </summary>
        /// <param name="month"> The month being searched </param>
        /// <param name="response"> The response being created </param>
        /// <param name="logger"> Used to log any errors or success messages </param>
        ///

        private void MatchProcedures(int month, Dictionary<BillableProcedure, List<string>> response, Logging logger = null)
        {
            //Create a dictionary of pks
            Dictionary<BillableProcedure, List<int>> pks = BillingDBAsDict(month);

            //Set information to variable
            int year = CalendarManager.ConvertMonthToYear(ref month);

            //Loop through each billable procedure
            foreach(BillableProcedure bp in response.Keys)
            {
                //If the month or the year doesn't match
                if(bp.month != month || bp.year != year)
                {
                    logger?.Log(LoggingInfo.ErrorLevel.ERROR, "Invalid date for response");
                    continue;
                }
                
                //If the response doesn't match
                if(!response.ContainsKey(bp))
                {
                    logger?.Log(LoggingInfo.ErrorLevel.WARN, "Could not match " + bp + " to known procedures");
                    continue;
                }

                //If the count of pks doesn't match the count of responses
                if(pks[bp].Count != response[bp].Count)
                {
                    logger?.Log(LoggingInfo.ErrorLevel.ERROR, "Billable procedure response and database data mismatched for procedure " + bp);
                    continue;
                }

                //Save information into a variable
                var zipped = response[bp].Zip(pks[bp], (s, i) => new Tuple<int, string>(i, s));

                //Loop through each procedure
                foreach(Tuple<int, string> procedure in zipped)
                {
                    procedures[procedure.Item1, "CodeResponse"] = (BillingCodeResponse) Enum.Parse(typeof(BillingCodeResponse), procedure.Item2);
                }

                //Log the success
                logger?.Log(LoggingInfo.ErrorLevel.INFO, "Successfully merged billable procedures for " + pks[bp] + " and " + response[bp]);
            }

            //Log that done
            logger?.Log(LoggingInfo.ErrorLevel.INFO, "Finished merging billable procedure responses");
        }

        /// <summary>
        /// Parses the data
        /// </summary>
        /// <param name="data"> string array of data </param>
        /// <param name="logger"> Logger used to log any errors or success messages </param>
        /// <returns>Dictionary with information</returns>
        ///

        private Dictionary<BillableProcedure, List<string>> ParseData(string[] data, Logging logger = null)
        {
            //Create a new dictionary
            Dictionary<BillableProcedure, List<string>> _data = new Dictionary<BillableProcedure, List<string>>();

            //Loop through each line in string array
            foreach (string line in data)
            {
                try
                {
                    //Parse the line
                    BillableProcedure bp = ParseProcedure(line);

                    //Create a list to store info
                    List<string> codes = _data.ContainsKey(bp) ? _data[bp] : new List<string>();

                    //Add the info to the list
                    codes.Add(bp.response);

                    //Save the list to dictionary
                    _data[bp] = codes;
                }

                //If an exception is thrown
                catch (ArgumentException e)
                {
                    logger?.Log(LoggingInfo.ErrorLevel.ERROR, e.Message);
                    continue;
                }
            }

            return _data;
        }

        /// <summary>
        /// Parses the procedure
        /// </summary>
        /// <param name="line"> The line being parsed </param>
        /// <returns>BillableProcedure with information</returns>
        ///

        private BillableProcedure ParseProcedure(string line)
        {
            //Validate the line
            Match match = Regex.Match(line, BP_REGEX);
            
            //If the line is invalid
            if(!match.Success)
            {
                throw new ArgumentException("Invalid line '" + line + "'");
            }

            //Instantiate a new Billable Procedure
            BillableProcedure bp = new BillableProcedure();

            //Parse and store the information
            bp.year = int.Parse(match.Groups["year"].Value);
            bp.month = int.Parse(match.Groups["month"].Value);
            bp.day = int.Parse(match.Groups["day"].Value);

            bp.HCN = match.Groups["hcn"].Value;
            bp.sex = match.Groups["sex"].Value[0];
            bp.code = match.Groups["code"].Value;
            bp.fee = match.Groups["fee"].Value;

            bp.response = match.Groups["resp"].Value;

            return bp;
        }

        /// <summary>
        /// Gets information from hashset
        /// </summary>
        /// <param name="set"> The hashset contain billable procedure </param>
        /// <returns>Dictionary containing information</returns>
        ///

        private Dictionary<Tuple<AptTimeSlot, string, string>, List<BillableProcedure>> FromHashset(HashSet<BillableProcedure> set)
        {
            //Create a new dictionary
            Dictionary<Tuple<AptTimeSlot, string, string>, List<BillableProcedure>> dict =
                new Dictionary<Tuple<AptTimeSlot, string, string>, List<BillableProcedure>>();

            //Loop through each billable procedure
            foreach(BillableProcedure bp in set)
            {
                //Create the slot with information
                AptTimeSlot slot = new AptTimeSlot(bp.month, bp.day, 0);

                //Create the new key containing information from slot
                Tuple<AptTimeSlot, string, string> key = new Tuple<AptTimeSlot, string, string>(slot, bp.HCN, bp.code);

                //Create a new list
                List<BillableProcedure> pks = dict.ContainsKey(key) ?
                    dict[key] : new List<BillableProcedure>();

                //Add info to list
                pks.Add(bp);

                //Save information from list into dictionary
                dict[key] = pks;
            }

            return dict;
        }

        /// <summary>
        /// Reinterperits the billable procedure table as a dictionary between
        /// a billable procedure (one per day:patient:code) and a list of the pks
        /// for each procedure which share the attributes.
        /// </summary>
        /// <param name="target_month">The current month</param>
        /// <returns>The dictionary.</returns>
        private Dictionary<BillableProcedure, List<int>> BillingDBAsDict(int target_month)
        {
            //Creates a new dictionary
            Dictionary<BillableProcedure, List<int>> dict = new Dictionary<BillableProcedure, List<int>>();

            //Loop through the keys
            foreach (object pk in procedures.Keys)
            {
                //Create a new billable procedure
                BillableProcedure bp = new BillableProcedure();

                //Save the aptid
                int aptid = (int)procedures[pk, "AppointmentID"];

                //Save the month
                int month = (int)appointments[aptid, "Month"];

                //If the month isnt the current month
                if(month != target_month)
                {
                    continue;
                }
                
                //Obtain all of the information needed
                bp.year = CalendarManager.ConvertMonthToYear(ref month);
                bp.month = month;
              
                bp.day = (int)appointments[aptid, "Day"];
                
                bp.HCN = (string)people[appointments[aptid, "PatientID"], "HCN"];
              
                bp.code = (string)procedures[pk, "BillingCode"];
              
                bp.sex = people[appointments[aptid, "PatientID"], "sex"].ToString()[0];
              
                bp.fee = (string)billingMaster[bp.code, "DollarAmount"];
                
                //Create a list that will hold this information
                List<int> pks = dict.ContainsKey(bp) ? dict[bp] : new List<int>();

                //Add the information to the list
                pks.Add((int)pk);

                //Save the information into a dictionary
                dict[bp] = pks;
            }

            return dict;
        }

    }   
}
