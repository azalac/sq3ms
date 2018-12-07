using Definitions;
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
        public Logging logger = new Logging();

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
            object pk = People.WhereEquals<string>("firstName", name).First();
            int pID = 0;

            if (pk != null)
            {
                pID = (int)pk;
            }
            else
            {
                logger.Log(Definitions.LoggingInfo.ErrorLevel.WARN, "Patient Not Found");
            }

            return 0;
        }

        /// <summary>
        /// Gets the full name of a patient.
        /// </summary>
        /// <param name="patient_id">The patient's ID</param>
        /// <returns>The firstname + The initial + The lastname</returns>
        public string GetFullName(int patient_id)
        {
            string fullName = string.Empty;
            object pk = People.WhereEquals<int>("PatientID", patient_id).First();
            
            if(pk != null)
            {
                object lastName = People[pk, "lastName"];
                object firstName = People[pk, "firstName"];
                object mInitial = People[pk, "mInitial"];

                fullName = (string)firstName + (string)mInitial + (string)lastName;
            }
            else 
            {
                logger.Log(Definitions.LoggingInfo.ErrorLevel.WARN, "Patient Name Not Found");
            }
            

            return fullName;
        }

        /// <summary>
        /// Gets the full name of a patient.
        /// </summary>
        /// <param name="HCN">The patient's health card number</param>
        /// <param name="lastName">The patient's last name</param>
        /// <param name="firstName">The patient's first name</param>
        /// <param name="mInitial">The patient's middle initial</param>
        /// <param name="dateBirth">The patient's dat of birth</param>
        /// <param name="sex">The patient's sex</param>
        /// <param name="houseID">The patient's house ID</param>
        /// <returns>The firstname + The initial + The lastname</returns>
        public void CreatePatient(string HCN, string lastName, string firstName, char mInitial, string dateBirth, SexTypes sex, int houseID)
        {
            int maxVal = People.GetMaximum("PatientID");
            

            try
            {
                People.Insert(maxVal + 1, HCN, lastName, firstName, mInitial, dateBirth, sex, houseID);
            }
            catch
            {
                logger.Log(Definitions.LoggingInfo.ErrorLevel.WARN, "Patient was Not Created");
            }

        }


        //*******************ARE WE DOING UPDATE PATIENT*********************
    }

        
}
