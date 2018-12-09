/*
* FILE          : Billing.cs
* PROJECT       : INFO-2180 Software Quality 1, Term Project
* PROGRAMMER    : Blake Ribble and Austin Zalac
* FIRST VERSION : November 23, 2018
*/

using System.Collections.Generic;
using Support;
using System.Linq;

namespace Billing
{
    /// <summary>
    /// Handles billing code database interaction.
    /// </summary>
	public class Billing
    {

        //Create class level variables
        private readonly DatabaseTable BillingEntries;
        private readonly DatabaseTable BillingMaster;
        private readonly DatabaseTable Appointments;
        private readonly DatabaseTable Patients;

        /// <summary>
        /// Constructor that initializes class level variables
        /// </summary>
        /// <param name="database"> Used to obtain the information from database</param>
        /// 
        public Billing(DatabaseManager database)
        {
            BillingEntries = database["Billing"];
            BillingMaster = database["BillingMaster"];
            Appointments = database["Appointments"];
            Patients = database["Patients"];

        }

        /// <summary>
        /// Adds a billing code to the given appointment
        /// </summary>
        /// <param name="AppointmentID">The appointment</param>
        /// <param name="code">The billing code</param>
		public void AddBillingCode(int AppointmentID, string code)
        {
            //Get billingID
            int billingID = BillingEntries.GetMaximum("BillingID") + 1;

            //Get Key of Appointments row
            object key = Appointments.WhereEquals("AppointmentID", AppointmentID).First();

            int.TryParse(key.ToString(), out int AppPK);

            //Get month week day from appointment
            int.TryParse(Appointments[AppPK, "Month"].ToString(), out int month);
            int.TryParse(Appointments[AppPK, "Week"].ToString(), out int week);
            int.TryParse(Appointments[AppPK, "Day"].ToString(), out int day);
            string date = (month + ", " + week + ", " + day);

            //Get key of Patients where PatientID = PatientID in Appointments
            key = Patients.WhereEquals("PatientID", Appointments[AppPK, "PatientID"]).First();

            //Parse information
            int.TryParse(key.ToString(), out int Ppk);

            //Get HCN and sex
            string HCN = Patients[Ppk, "HCN"].ToString();
            Definitions.SexTypes sex = (Definitions.SexTypes)Patients[Ppk, "sex"];

            //Get key of BillingMaster where BillingCode = code
            key = BillingMaster.WhereEquals("BillingCode", code).First();

            //Get fee from BillinMaster
            string fee = BillingMaster[key, "DollarAmount"].ToString();

            //Set the ministry reponse to none
            object codeResponse = Definitions.BillingCodeResponse.NONE;

            //Insert the information into the database
            BillingEntries.Insert(billingID, AppointmentID, date, HCN, sex, code, fee, codeResponse);            
        }

        /// <summary>
        /// Removes Billing code
        /// </summary>
        /// <param name="AppointmentID">The appointment</param>
        /// <param name="code">The billing code</param>
        public void RemoveBillingCode(int AppointmentID, string code)
        {
            //Loop through each key and delete the row if value is the same as what is being searched for
            foreach (object key in BillingEntries.WhereEquals("AppointmentID", AppointmentID))
            {
                if (BillingEntries[key, "BillingCode"].ToString() == code)
                {
                    BillingEntries.DeleteRow(key);
                }
            }
        }

        /// <summary>
        /// Gets all billing entries for a given appointment.
        /// </summary>
        /// <param name="AppointmentID">The appointment</param>
        /// <returns>All billing entries for the appointment</returns>
        public IEnumerable<int> FindBillingEntries(int AppointmentID)
        {
            //Loop through each entry to check value that matches appointment ID by searched for
            foreach (object key in BillingEntries.WhereEquals("AppointmentID", AppointmentID))
            {
                int.TryParse(key.ToString(), out int pk);
                yield return pk;
            }
            yield break;
        }
    }
}

