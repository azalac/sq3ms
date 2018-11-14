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
            InterfaceStart.InitConsole();

            InterfaceStart _interface = new InterfaceStart(StandardConsole.INSTANCE);

            _interface.WaitUntilExit();

            InterfaceStart.ResetConsole();
        }
    }
}
