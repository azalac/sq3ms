using System;
using System.Collections.Generic;
using Support;

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

		public Billing (DatabaseManager database)
		{
			BillingEntries = database ["Billing"];
		}

        /// <summary>
        /// Adds a billing code to the given appointment
        /// </summary>
        /// <param name="AppointmentID">The appointment</param>
        /// <param name="code">The billing code</param>
		public void AddBillingCode(int AppointmentID, string code)
		{

		}

        /// <summary>
        /// Removes a billing code for a given appointment
        /// </summary>
        /// <param name="AppointmentID">The appointment</param>
        /// <param name="code">The billing code</param>
        public void RemoveBillingCode(int AppointmentID, string code)
        {

        }

        /// <summary>
        /// Gets all billing entries for a given appointment.
        /// </summary>
        /// <param name="AppointmentID">The appointment</param>
        /// <returns>All billing entries for the appointment</returns>
        public IEnumerable<int> FindBillingEntries(int AppointmentID)
        {
            // if you want, you can implement this by using 'yield' in order to speed up searching
            // see https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/yield

            return null;
        }
    }
}

