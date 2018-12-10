/*
* FILE          : Person.cs
* PROJECT       : INFO-2180 Software Quality 1, Term Project
* PROGRAMMER    : Mike Ramoutsakis
* FIRST VERSION : November 14, 2018
*/
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
        private DatabaseTable Households;
        public Logging logger = new Logging();

        public PersonDB(DatabaseManager database)
        {
            People = database["Patients"];
            Households = database["Household"];
        }
        
        /// <summary>
        /// Finds a person by various fields. If a field is null, it is ignored.
        /// </summary>
        /// <param name="firstname">The firstname.</param>
        /// <param name="initial">The middle initial.</param>
        /// <param name="lastname">The lastname.</param>
        /// <param name="phonenumber">The phonenumber.</param>
        /// <param name="hcn">The health card number/</param>
        /// <returns>All people who match</returns>
        public IEnumerable<int?> Find(string firstname, char? initial, string lastname, string phonenumber, string hcn)
        {
            List<string> columns = new List<string>();
            List<object> values = new List<object>();

            if (firstname != null)
            {
                columns.Add("firstName");
                values.Add(firstname);
            }

            if (initial != null)
            {
                columns.Add("mInitial");
                values.Add(initial);
            }

            if (lastname != null)
            {
                columns.Add("lastName");
                values.Add(lastname);
            }
            
            if (hcn != null)
            {
                columns.Add("HCN");
                values.Add(hcn);
            }

            var matches = People.WhereEquals(string.Join(";", columns.ToArray()), values.ToArray());

            HashSet<int?> people = new HashSet<int?>(matches.Select(pk => new int?((int)pk)));
            
            if (phonenumber != null)
            {
                HashSet<int?> valid = new HashSet<int?>();

                foreach (int person in people)
                {
                    string phone = (string)Households[People[person, "HouseID"], "numPhone"];

                    if(Equals(phone, phonenumber))
                    {
                        valid.Add(person);
                    }
                }

                return valid;
            }

            return people.AsEnumerable();
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
        public object CreatePatient(string HCN, string lastName, string firstName, char mInitial, string dateBirth, SexTypes sex, int houseID)
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

            return maxVal + 1;
        }


        
    }

        
}
