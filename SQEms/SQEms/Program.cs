using SchedulingUI;
using Definitions;
using Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Configuration;

namespace SQEms
{
    class Program
    {
        static void Main(string[] args)
		{
            DatabaseManager database = new DatabaseManager();

            database.LoadAll();

            DatabaseTable patients = database["Patients"];

            DatabaseTable appointments = database["Appointments"];

            DatabaseTable master = database["BillingMaster"];

            DatabaseTable billing = database["Billing"];
            
            
            //Create first patient
            patients.Insert(1, "5534567890EE", "Parmenter", "Billy", 'A', "May 6, 1996", SexTypes.M, 1);

            //Create first appointment
            appointments.Insert(1, 10, 3, 0, 1, 1);
            //Create first billing row
            billing.Insert(1, 1, "A665", BillingCodeResponse.NONE);

            //Second appointment
            appointments.Insert(2, 10, 5, 0, 1, 1);
            //second row
            billing.Insert(2, 2, "A005", BillingCodeResponse.NONE);

            //second patient
            patients.Insert(2, "1234567890KV", "Blanski", "Bob", 'A', "May 6, 1996", SexTypes.F, 1);

            //third appointment
            appointments.Insert(3, 10, 5, 0, 1, 1);

            //third and fourth rows (both assumed in same appointment)
            billing.Insert(3, 3, "A665", BillingCodeResponse.NONE);
            billing.Insert(4, 3, "A005", BillingCodeResponse.NONE);



            InterfaceStart.InitConsole();

            InterfaceStart _interface = new InterfaceStart(StandardConsole.INSTANCE, database);

            _interface.WaitUntilExit();

            InterfaceStart.ResetConsole();

            database.SaveAll();
        }
    }
}
