using Billing;
using Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulingUI
{
    /// <summary>
    /// A wrapper which contains an instance of all database interaction classes,
    /// with methods to help bridge between them and the interface.
    /// </summary>
    public class DatabaseWrapper
    {

        private AppointmentScheduler scheduler;
        private Billing.Billing billing;
        private BillingFileInteraction billingIO;
        private Demographics.PersonDB people;

        public DatabaseWrapper(DatabaseManager database)
        {
            scheduler = new AppointmentScheduler(database);
            billing = new Billing.Billing(database);
            billingIO = new BillingFileInteraction(database);
            people = new Demographics.PersonDB(database);
        }

        public int? GetAppointmentCount(int month, int day)
        {
            return scheduler.AppointmentCount(month, day);
        }

        public Tuple<int, int, int>[] GetAppointmentsOnDay(int month, int day)
        {
            return scheduler.GetPatientIDs_AllDay(new AptTimeSlot(month, day, 0));
        }

        /// <summary>
        /// Finds people based on the given arguments.
        /// </summary>
        /// <param name="firstname">The firstname.</param>
        /// <param name="initial">The middle initial.</param>
        /// <param name="lastname">The lastname.</param>
        /// <param name="phonenumber">The phonenumber.</param>
        /// <param name="hcn">The health card number/</param>
        /// <returns>All people who match</returns>
        public IEnumerable<int> FindPerson(string firstname, char? initial, string lastname, string phonenumber, string hcn)
        {
            return people.Find(firstname, initial, lastname, phonenumber, hcn);
        }

        public int AddPerson(string firstname, char initial, string lastname, string phonenumber, string hcn, int houseid)
        {
            return 0;
        }
        
        /// <summary>
        /// Gets the billing codes for an appointment.
        /// </summary>
        /// <param name="appointmentid">The appointment.</param>
        /// <returns>The codes.</returns>
        public string[] GetBillingCodesForApt(int appointmentid)
        {
            return billing.GetBillableProceduresFor(appointmentid).ToArray();
        }

        /// <summary>
        /// Sets the codes for a given appointment by determining the deltas
        /// and removing them or adding them, depending on how they are delta-d.
        /// 
        /// Does nothing if the codes haven't changed.
        /// </summary>
        /// <param name="appointmentid">The appointment to update.</param>
        /// <param name="codes">The codes to set.</param>
        public void SetBillingCodesForApt(int appointmentid, string[] codes)
        {
            HashSet<string> current = new HashSet<string>(GetBillingCodesForApt(appointmentid));

            HashSet<string> requested = new HashSet<string>(codes);

            // ignore any codes that haven't changed
            HashSet<string> intersection = new HashSet<string>(current.Intersect(requested));
            foreach(string intersect in intersection)
            {
                current.Remove(intersect);
                requested.Remove(intersect);
            }

            // codes which are in current, but not requested should be removed
            foreach(string remove in current)
            {
                billing.RemoveBillingCode(appointmentid, remove);
            }

            // codes which are in requested, but not current should be added
            foreach(string add in current)
            {
                billing.AddBillingCode(appointmentid, add);
            }
        }
        
        public void GenerateBillingFile(int month, string path)
        {
            billingIO.GenerateMonthlyBillingFile(month, path);
        }

        public bool DoBillingReconcile(int month, string responsepath)
        {
            return billingIO.ParseResponse(month, responsepath);
        }

    }
}
