using System;
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

        /*
         * I'm not sure about the proper way to go about billing handling, so I
         * just stuck these methods in here. There's two ways I could do it. The first
         * is this way, with many billing codes with a foreign key to the appointment.
         * The second is to have a single billing entry and with two fields, a string[]
         * and a BillingCodeResponse[].
         * 
         * The second would be faster, but at our sample sizes, it would be millisecond
         * differences.
         */

        private readonly DatabaseTable BillingEntries;
        private readonly DatabaseTable BillingMaster;
        private readonly DatabaseTable Appointments;
        private readonly DatabaseTable Patients;


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

            int.TryParse(key.ToString(), out int Ppk);

            //Get HCN and sex
            string HCN = Patients[Ppk, "HCN"].ToString();
            Definitions.SexTypes sex = (Definitions.SexTypes)Patients[Ppk, "sex"];

            //Get key of BillingMaster where BillingCode = code
            key = BillingMaster.WhereEquals("BillingCode", code).First();

            //Get fee from BillinMaster
            string fee = BillingMaster[key, "DollarAmount"].ToString();








            // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! NULL breaks code What should be here??????????????????????????????????????








            object codeResponse = Definitions.BillingCodeResponse.PAID;

            BillingEntries.Insert(billingID, AppointmentID, date, HCN, sex, code, fee, codeResponse);
        }

        /// <summary>
        /// Removes Billing code
        /// </summary>
        /// <param name="AppointmentID">The appointment</param>
        /// <param name="code">The billing code</param>
        public void RemoveBillingCode(int AppointmentID, string code)
        {
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

            foreach (object key in BillingEntries.WhereEquals("AppointmentID", AppointmentID))
            {
                int.TryParse(key.ToString(), out int pk);
                yield return pk;
            }

            yield break;
        }
    }
}

