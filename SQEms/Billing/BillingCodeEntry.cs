using Demographics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Definitions;

namespace Billing
{
    public struct BillingCodeEntry
    {
        public string DateOfService;

        public string HCN;

        public SexTypes Sex;

        public string BillingCode;

        public string Fee;

        public BillingCodeResponse Response;

        /// <summary>
        /// Parses a BillingCodeEntry from a string.
        /// </summary>
        /// <remarks>
        /// 
        /// The first 8 characters are the DateOfService (YYYYMMDD).
        /// The next 12 characters are the patient's HCN (10 numeric, 2 alphabetic).
        /// The next 1 character is the patient's sex (M/F/I/H). <see cref="SexTypes"/>
        /// The next 11 characters are the Fee (7 integer, 4 decimal, no period, zero padded).
        /// 
        /// </remarks>
        /// <param name="str">The string</param>
        /// <returns>The BillingCodeEntry</returns>
        public static BillingCodeEntry ParseFromString(string str)
        {
            //TODO this
            return default(BillingCodeEntry);
        }

        /// <summary>
        /// Converts this Billing Code Entry to a string. <see cref="BillingCodeEntry.ParseFromString(string)"/>
        /// </summary>
        /// <returns>This, as a string</returns>
        public override string ToString()
        {
            //TODO this
            return null;
        }
    }

}
