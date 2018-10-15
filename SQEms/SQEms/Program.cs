using SchedulingUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQEms
{
    class Program
    {
        static void Main(string[] args)
        {
			RootContainer root = new RootContainer (StandardConsole.INSTANCE);

			FlowContainer container = new FlowContainer ();

			container.Add (new Box (), new Box (), new Box ());

			root.Add (container);

			root.DoLayout ();

			root.Draw ();

			Console.ReadKey (true);

        }
    }
}
