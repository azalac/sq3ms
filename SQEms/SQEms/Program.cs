using SchedulingUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace SQEms
{
    class Program
    {
        static void Main(string[] args)
		{
			RootContainer root = new RootContainer (StandardConsole.INSTANCE);

			InputArea inputs = new InputArea ("one", "two", "three")
			{
				RowHeight = 2,
                InputWidth = 20
			};

			root.Add (inputs);

			inputs.DoLayout ();
			root.DoLayout ();
			root.Draw ();

			KeyboardInput input = new KeyboardInput (root)
			{
				ExitKey = ConsoleKey.Escape
			};

			Console.CursorVisible = false;

			input.StartThread ();

			input.InternalThread.Join ();

			Console.Clear ();
			Console.ResetColor ();
			Console.CursorVisible = true;
		}
    }
}
