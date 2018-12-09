using SchedulingUI;
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

            InterfaceStart.InitConsole();

            InterfaceStart _interface = new InterfaceStart(StandardConsole.INSTANCE, database);

            _interface.WaitUntilExit();

            InterfaceStart.ResetConsole();

            database.SaveAll();
        }
    }
}
