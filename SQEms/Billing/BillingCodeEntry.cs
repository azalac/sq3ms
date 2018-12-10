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
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Billing
{
    struct BillableProcedure
    {
        public int year, month, day;
        public string HCN;
        public char sex;
        public string code;
        public string fee;
        public string response;

        public override bool Equals(object obj)
        {
            if (!(obj is BillableProcedure))
            {
                return false;
            }

            var procedure = (BillableProcedure)obj;
            return year == procedure.year &&
                   month == procedure.month &&
                   day == procedure.day &&
                   HCN == procedure.HCN &&
                   sex == procedure.sex &&
                   code == procedure.code;
        }

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
        /// <param name="aptID"> The appointment ID</param>
        /// <param name="procID"> The proceduer ID</param>
        public string GenerateBillableProcedureLine(int appointment, int procedure)
        {
            // Blake, sorry for mangling your function, but it didn't do what it needs to.

            int month = (int)appointments[appointment, "Month"];
            DateTime date = new DateTime(CalendarManager.ConvertMonthToYear(ref month), month, (int)appointments[appointment, "Day"]);

            object patient_pk = appointments[appointment, "PatientID"];

            string HCN = (string)people[patient_pk, "HCN"];

            SexTypes sex = (SexTypes)people[patient_pk, "sex"];

            string code = (string)procedures[procedure, "BillingCode"];

            string price = (string)billingMaster[code, "DollarAmount"];

            return date.ToString("YYYYMMDD") + HCN + sex.ToString() + code + price;
        }
        
        public void GenerateMonthlyBillingFile(int month, string path)
        {
            FileIO.WriteAllBillableProcedures(path, appointments, procedures, month, GenerateBillableProcedureLine);
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
            foreach(object key in procedures.WhereEquals("Month", month))
            {
                totalEncounters++;

                //Convert the value
                float.TryParse(procedures[key, "Fee"].ToString(), out float billed);
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
            StringBuilder saveToFile = new StringBuilder();
            saveToFile.AppendFormat("Total Encounters Billed: {0}\n" +
                                    "Total Billed Procedures: {1}\n" +
                                    "Received Total: {2}\n" +
                                    "Received Percentage: {3}\n" +
                                    "Average Billing: {4}\n" +
                                    "Encounters To Follow-up: {5}\n", 
                                    totalEncounters, billedProcedures, receivedTotal, receivedPercentage, averageBilling, toFollowEncounters);
            
        }
        
        /// <summary>
        /// Parse the response code lines in file provided
        /// </summary>
        /// <param name="original"> The original data </param>
        /// <param name="response"> The response data </param>
        ///
        public bool ParseResponse(int month, string path)
        {
            //Create a new instance of the log class - used for errors
            Logging logger = new Logging();

            string[] data = FileIO.GetResponseFileData(path);

            if(data == null)
            {
                return false;
            }

            MatchProcedures(month, ParseData(data, logger), logger);

            return true;
        }
        
        private void MatchProcedures(int month, Dictionary<BillableProcedure, List<string>> response, Logging logger = null)
        {
            Dictionary<int, List<int>> pks = BillingDBAsDict(month);

            foreach(BillableProcedure bp in response.Keys)
            {
                if(pks[bp.GetHashCode()].Count != response[bp].Count)
                {
                    logger?.Log(LoggingInfo.ErrorLevel.ERROR, "Billable procedure response and database data mistmatch for procedure " + bp);
                    continue;
                }

                var zipped = response[bp].Zip(pks[bp.GetHashCode()], (s, i) => new Tuple<int, string>(i, s));

                foreach(Tuple<int, string> procedure in zipped)
                {
                    procedures[procedure.Item1, "ResponseCode"] = procedure.Item2;
                }

                logger?.Log(LoggingInfo.ErrorLevel.INFO, "Successfully merged billable procedures for " + pks[bp.GetHashCode()] + " and " + response[bp]);
            }

            logger?.Log(LoggingInfo.ErrorLevel.INFO, "Finished merging billable procedure responses");
        }

        private Dictionary<BillableProcedure, List<string>> ParseData(string[] data, Logging logger = null)
        {
            Dictionary<BillableProcedure, List<string>> _data = new Dictionary<BillableProcedure, List<string>>();

            foreach (string line in data)
            {
                try
                {
                    BillableProcedure bp = ParseProcedure(line);

                    List<string> codes = _data.ContainsKey(bp) ? _data[bp] : new List<string>();

                    codes.Add(bp.code);

                    _data[bp] = codes;
                }
                catch (ArgumentException e)
                {
                    logger?.Log(LoggingInfo.ErrorLevel.ERROR, e.Message);
                    continue;
                }
            }

            return _data;
        }

        private BillableProcedure ParseProcedure(string line)
        {
            Match match = Regex.Match(line, BP_REGEX);
            
            if(!match.Success)
            {
                throw new ArgumentException("Invalid line '" + line + "'");
            }

            BillableProcedure bp = new BillableProcedure();

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

        private Dictionary<Tuple<AptTimeSlot, string, string>, List<BillableProcedure>> FromHashset(HashSet<BillableProcedure> set)
        {
            Dictionary<Tuple<AptTimeSlot, string, string>, List<BillableProcedure>> dict =
                new Dictionary<Tuple<AptTimeSlot, string, string>, List<BillableProcedure>>();

            foreach(BillableProcedure bp in set)
            {
                AptTimeSlot slot = new AptTimeSlot(bp.month, bp.day, 0);

                Tuple<AptTimeSlot, string, string> key = new Tuple<AptTimeSlot, string, string>(slot, bp.HCN, bp.code);

                List<BillableProcedure> pks = dict.ContainsKey(key) ?
                    dict[key] : new List<BillableProcedure>();

                pks.Add(bp);

                dict[key] = pks;
            }

            return dict;
        }

        /// <summary>
        /// Reinterperits the billable procedure table as a dictionary between
        /// a billable procedure (one per day:patient:code) and a list of the pks
        /// for each procedure which share the attributes.
        /// </summary>
        /// <remarks>
        /// 
        /// My idea for this is to do the same as this, but instead of pks, it's
        /// a list of response codes. Then, each code *should* have a matching
        /// pk, which will allow me to mark specific billable procedures as
        /// invalid.
        /// 
        /// </remarks>
        /// <param name="target_month">The current month</param>
        /// <returns>The dictionary.</returns>
        private Dictionary<int, List<int>> BillingDBAsDict(int target_month)
        {
            Dictionary<int, List<int>> dict = new Dictionary<int, List<int>>();

            foreach (object pk in procedures.Keys)
            {

                BillableProcedure bp = new BillableProcedure();

                int aptid = (int)procedures[pk, "AppointmentID"];

                int month = (int)appointments[aptid, "Month"];

                if(month != target_month)
                {
                    continue;
                }
                
                bp.year = CalendarManager.ConvertMonthToYear(ref month);
                bp.month = month;

                bp.day = (int)appointments[aptid, "Day"];
                
                bp.HCN = (string)people[appointments[aptid, "PatientID"], "HCN"];
                bp.code = (string)procedures[pk, "BillingCode"];

                bp.sex = people[appointments[aptid, "PatientID"], "sex"].ToString()[0];

                bp.fee = (string)billingMaster[bp.code, "DollarAmount"];

                int bpHash = bp.GetHashCode();

                List<int> pks = dict.ContainsKey(bpHash) ? dict[bpHash] : new List<int>();

                pks.Add((int)pk);

                dict[bpHash] = pks;
            }

            return dict;
        }

    }   
}
