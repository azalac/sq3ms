using Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Demographics
{
    /// <summary>
    /// The interaction between the interface and the database.
    /// Allows the interface to query patients and get their information.
    /// </summary>
    public class PersonDB
    {
        private DatabaseTable People;

        public PersonDB(DatabaseManager database)
        {
            People = database["Patients"];
        }

        /// <summary>
        /// Finds a person by name 
        /// </summary>
        /// <param name="name">The name of the patient</param>
        /// <param name="DateOfBirth">The optional DateOfBirth</param>
        /// <returns>The patient ID</returns>
        public int Find(string name, string DateOfBirth = null)
        {
            return 0;
        }

        /// <summary>
        /// Gets the full name of a patient.
        /// </summary>
        /// <param name="patient_id">The patient's ID</param>
        /// <returns>The firstname + The initial + The lastname</returns>
        public string GetFullName(int patient_id)
        {
            return null;
        }

    }
}
