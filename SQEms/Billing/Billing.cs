using System;
using Support;

namespace Billing
{
	public class Billing
	{
		DatabaseTable BillingEntries;

		public Billing (DatabaseManager database)
		{
			BillingEntries = database ["Billing"];
		}

		public void SetBillingCodes(int AppointmentID, params string[] codes)
		{

		}
	}
}

