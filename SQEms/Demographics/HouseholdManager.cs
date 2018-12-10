/*
* FILE          : HouseholdManager.cs
* PROJECT       : INFO-2180 Software Quality 1, Term Project
* PROGRAMMER    : Mike Ramoutsakis
* FIRST VERSION : November 20, 2018
*/

using Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Demographics
{
    public class HouseholdManager
    {
        //Create database variables
        private DatabaseTable households;
        private DatabaseTable people;

        /// <summary>
        /// Constructor that initializes class level variables
        /// </summary>
        /// <param name="database"> Used to obtain the information from database</param>
        public HouseholdManager(DatabaseManager database)
        {
            households = database["Household"];
            people = database["Patients"];
        }

        /// <summary>
        /// Method to find household info
        /// </summary>
        /// <param name="address1"> Contains the first address line</param>
        /// <param name="address2"> Contains the second address line</param>
        /// <param name="city"> Contains city information </param>
        /// <param name="province"> Contains the province information</param>
        /// <param name="numPhone"> Contains the phone number</param>
        /// <returns> object - household info</returns>
        /// 
        public object FindHousehold(string address1, string address2, string city,
            string province, string numPhone)
        {
            //Create lists
            List<string> columns = new List<string>();
            List<object> values = new List<object>();

            //If address 1 is not null, add to lists
            if(address1 != null)
            {
                columns.Add("addressLine1");
                values.Add(address1);
            }

            //If address 2 is not null, add to lists
            if (address2 != null)
            {
                columns.Add("addressLine2");
                values.Add(address2);
            }

            //If city is not null, add to lists
            if (city != null)
            {
                columns.Add("city");
                values.Add(city);
            }

            //If province is not null, add to lists
            if (province != null)
            {
                columns.Add("province");
                values.Add(province);
            }

            //If phone number is not null, add to lists
            if (numPhone != null)
            {
                columns.Add("numPhone");
                values.Add(numPhone);
            }

            return households.WhereEquals(string.Join(";", columns.ToArray()), values.ToArray());
        }

        /// <summary>
        /// Method that adds household into the database
        /// </summary>
        /// <param name="address1"> Contains the first address line</param>
        /// <param name="address2"> Contains the second address line</param>
        /// <param name="city"> Contains city information </param>
        /// <param name="province"> Contains the province information</param>
        /// <param name="numPhone"> Contains the phone number</param>
        /// <param name="HOH_HCN"> Contains the HCN of household</param>
        /// <returns>object - pk of household</returns>
        /// 
        public object AddHousehold(string address1, string address2, string city,
            string province, string numPhone, string HOH_HCN)
        {
            int pk = households.GetMaximum("HouseID") + 1;

            households.Insert(pk, address1, address2, city, province, numPhone, HOH_HCN);

            return pk;
        }

        /// <summary>
        /// Method that adds household into the database
        /// </summary>
        /// <param name="person"> Contains the person info</param>
        /// <param name="house"> Contains house info</param>
        /// 
        public void SetHousehold(int person, int house)
        {
            people[person, "HouseID"] = house;
        }
    }
}
