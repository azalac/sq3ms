/*
* FILE          : Billing.cs
* PROJECT       : INFO-2180 Software Quality 1, Term Project
* PROGRAMMER    : Blake Ribble 
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
            // sorry for mangling this, there was a lot of normalization issues with the db

            //Get billingID
            int billingID = BillingEntries.GetMaximum("BillingID") + 1;
            
            //Insert the information into the database
            BillingEntries.Insert(billingID, AppointmentID, code, Definitions.BillingCodeResponse.NONE);            
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
        /// Gets all billable procedures for a given appointment.
        /// </summary>
        /// <param name="appointment">The appointment</param>
        /// <returns>The procedure codes</returns>
        public IEnumerable<string> GetBillableProceduresFor(int appointment)
        {
            return BillingEntries.WhereEquals("AppointmentID", appointment)
                .Select(pk => (string)BillingEntries[pk, "BillingCode"]);
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
                yield return (int)key;
            }
            yield break;
        }
    }
}

