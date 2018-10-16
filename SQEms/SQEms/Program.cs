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

			FlowContainer container = new FlowContainer ();

			Label l = new Label () {
				Text = "asdasdasdasdasdasdasdasdasdasdsdadasdasdasd",
				PreferredWidth = 1,
				DoWrapping = true
			};

			container.Add (new Box () { PreferredWidth = 5 }, new Box () { PreferredWidth = 5 },
			               new Box () { PreferredWidth = 5 }, l);

			root.Add (container);

			root.DoLayout ();
			root.Draw ();

			root.KeyPress += (object sender, ConsoleKeyEventArgs e) => {
				l.Text = e.Key.KeyChar.ToString();
				root.Draw(l);
			};

			KeyboardInput input = new KeyboardInput (root)
			{
				ExitKey = ConsoleKey.Escape
			};

			input.StartThread ();

			input.InternalThread.Join ();

		}
    }
}
