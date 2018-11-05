using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Billing
{
    public class BillingMasterEntry
    {
        public string FeeCode { get; private set; }

        public string EffectiveDate { get; private set; }

        public string DollarAmount { get; private set; }
        
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
        public static BillingMasterEntry ParseFromString(string str)
        {
            //TODO this
            return null;
        }

        /// <summary>
        /// Converts this entry to a string. <see cref="BillingMasterEntry.ParseFromString(string)"/>
        /// </summary>
        /// <returns>This, as a string</returns>
        public override string ToString()
        {
            //TODO this
            return null;
        }
        
    }
}
