/*
* FILE          : DatabaseTables.cs
* PROJECT       : INFO-2180 Software Quality 1, Term Project
* PROGRAMMER    : Billy Parmenter
* FIRST VERSION : November 23, 2018
*/
using Definitions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Support
{

    /// <summary>
    /// An example table prototype.
    /// </summary>
    public class TestTable : DatabaseTablePrototype
    {

        #region implemented abstract members of DatabaseTablePrototype

        public TestTable() :
            base(2)
        {
            Name = "TestTable";

            Columns = new string[] { "pk", "one" };

            ColumnTypes = new Type[] { typeof(Int32), typeof(string) };

            PrimaryKeyIndex = 0;

            base.PostInit();
        }

        #endregion

    }



    /// <summary>
    /// The prototype for the patient table.
    /// </summary>
    /// <remarks>
    /// Fields:
    /// Int32 - PatientID (PK)
    /// string - HCN
    /// string - lastName
    /// string - firstName
    /// char - mInitial
    /// string- dateBirth
    /// SexTypes - sex
    /// Int32 - HouseID (FK for household)
    /// </remarks>
    public class PeopleTable : DatabaseTablePrototype
    {

        #region implemented abstract members of DatabaseTablePrototype

        public PeopleTable() :
            base(8)
        {
            Name = "Patients";

            Columns = new string[]{
                "PatientID",
                "HCN",
                "lastName",
                "firstName",
                "mInitial",
                "dateBirth",
                "sex",
                "HouseID"
            };

            ColumnTypes = new Type[] {
                typeof(Int32),
                typeof(string),
                typeof(string),
                typeof(string),
                typeof(char),
                typeof(string),
                typeof(SexTypes),
                typeof(Int32)
            };

            ColumnReaders[6] = (r) => (SexTypes)r.ReadInt32();

            ColumnWriters[6] = (r, o) => r.Write(Convert.ToInt32(o));

            PrimaryKeyIndex = 0;

            base.PostInit();
        }

        #endregion

    }

    /// <summary>
    /// The prototype for the appointment table.
    /// </summary>
    public class AppointmentTable : DatabaseTablePrototype
    {
        #region implemented abstract members of DatabaseTablePrototype

        public AppointmentTable() :
            base(7)
        {
            Name = "Appointments";

            Columns = new string[]{
                "AppointmentID",
                "Month",
                "Day",
                "TimeSlot",
                "PatientID",
                "CaregiverID"
            };

            ColumnTypes = new Type[] {
                typeof(Int32),
                typeof(Int32),
                typeof(Int32),
                typeof(Int32),
                typeof(Int32),
                typeof(Int32)
            };

            PrimaryKeyIndex = 0;

            base.PostInit();
        }

        #endregion


    }

    /// <summary>
    /// The prototype for the household table.
    /// </summary>
    public class HouseholdTable : DatabaseTablePrototype
    {
        #region implemented abstract members of DatabaseTablePrototype

        public HouseholdTable() :
            base(7)
        {
            Name = "Household";

            Columns = new string[]{
                "HouseID",
                "addressLine1",
                "addressLine2",
                "city",
                "province",
                "numPhone",
                "HeadOfHouseHCN"
            };

            ColumnTypes = new Type[] {
                typeof(Int32),
                typeof(string),
                typeof(string),
                typeof(string),
                typeof(string),
                typeof(string),
                typeof(string)
            };

            PrimaryKeyIndex = 0;

            base.PostInit();
        }

        #endregion


    }

    /// <summary>
    /// The table which represents all billing info.
    /// </summary>
    public class BillingMasterTable : DatabaseTablePrototype
    {
        #region implemented abstract members of DatabaseTablePrototype

        public BillingMasterTable() :
            base(3)
        {
            Name = "BillingMaster";

            Columns = new string[] { "BillingCode", "EffectiveDate", "DollarAmount" };

            ColumnTypes = new Type[] { typeof(string), typeof(string), typeof(string) };

            PrimaryKeyIndex = 0;

            ReadOnly = true;

            CustomReader = Billing.BillingMasterEntry.Initialize;

            base.PostInit();
        }
        
        #endregion

    }

    /// <summary>
    /// The table which represents all billable procedures.
    /// </summary>
    /// <remarks>
    /// Columns:
    /// 
    /// BillingID - Int32 - PK
    /// AppointmentID - Int32 - FK(Appointments)
    /// BillingCode - string - FK(BillingMaster)
    /// CodeResponse - BillingCodeResponse
    /// 
    /// </remarks>
    public class BillingCodeTable : DatabaseTablePrototype
    {

        public BillingCodeTable() : base(4)
        {
            Name = "Billing";

            Columns = new string[] { "BillingID",
                                     "AppointmentID",
                                     "BillingCode",
                                     "CodeResponse" };

            ColumnTypes = new Type[] {
                typeof(Int32),
                typeof(Int32),
                typeof(string),
                typeof(BillingCodeResponse)
            };
            
            ColumnReaders[3] = (r) => (BillingCodeResponse)r.ReadInt32();

            ColumnWriters[3] = (r, o) => r.Write(Convert.ToInt32(o));

            PrimaryKeyIndex = 0;

            base.PostInit();
        }
        
    }

}
