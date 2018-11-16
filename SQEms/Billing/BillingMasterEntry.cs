using Support;
using System;

namespace Billing
{
    /// <summary>
    /// Represents the master entry for a billing code description.
    /// </summary>
    public class BillingMasterEntry
    {
        /// <summary>
        /// The fee code for this billing description.
        /// </summary>
        public string FeeCode { get; private set; }

        /// <summary>
        /// The effective starting date for the billing description.
        /// </summary>
        public string EffectiveDate { get; private set; }

        /// <summary>
        /// The amount the procedure costs.
        /// </summary>
        public string DollarAmount { get; private set; }

        /// <summary>
        /// Reads the master description file into the database.
        /// </summary>
        /// <param name="path">The master file path</param>
        /// <param name="database">The database to insert into</param>
        /// <remarks>
        /// 
        /// Calls <see cref="ParseFromString(string)"/>.
        /// 
        /// Pass each line to the method. If the method throws an ArgumentException, 
        /// log the error and ignore the line.
        /// 
        /// </remarks>
        public static void Initialize(string path, DatabaseManager database)
        {
            // this table is not yet implemented
            DatabaseTable BillingDescription = database["BillingMaster"];
        }

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
        /// <exception cref="ArgumentException">If the entry is invalid.</exception>
        public static BillingMasterEntry ParseFromString(string str)
        {
            //TODO this
            return null;
        }
        
    }
}
