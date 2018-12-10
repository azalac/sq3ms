using Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Demographics
{
    public class HouseholdManager
    {
        private DatabaseTable households;

        public HouseholdManager(DatabaseManager database)
        {
            households = database["Household"];
        }

        public object FindHousehold(string address1, string address2, string city,
            string province, string numPhone)
        {
            List<string> columns = new List<string>();
            List<object> values = new List<object>();

            if(address1 != null)
            {
                columns.Add("addressLine1");
                values.Add(address1);
            }

            if (address2 != null)
            {
                columns.Add("addressLine2");
                values.Add(address2);
            }

            if (city != null)
            {
                columns.Add("city");
                values.Add(city);
            }

            if (province != null)
            {
                columns.Add("province");
                values.Add(province);
            }

            if (numPhone != null)
            {
                columns.Add("numPhone");
                values.Add(numPhone);
            }

            return households.WhereEquals(string.Join(";", columns.ToArray()), values.ToArray());
        }

        public object AddHousehold(string address1, string address2, string city,
            string province, string numPhone, string HOH_HCN)
        {
            int pk = households.GetMaximum("HouseID") + 1;

            households.Insert(pk, address1, address2, city, province, numPhone, HOH_HCN);

            return pk;
        }

    }
}
