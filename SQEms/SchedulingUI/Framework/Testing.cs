using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulingUI
{
	public static class Testing
	{
		public static void TestInterface()
		{
			RootContainer root = new RootContainer (StandardConsole.INSTANCE);

			TabbedPane pane = new TabbedPane ();

			InputArea inputs = new InputArea ("one", "two", "three")
			{
				RowHeight = 2,
				InputWidth = 20
			};

			Button s = new Button ()
			{
				Text = "Submit"
			};

			Button s2 = new Button ()
			{
				Text = "Submit"
			};

			GridContainer flow = new GridContainer ()
			{
				CountY = 2
			};

			s.Action += (object sender, EventArgs e) => {
				pane.SelectedIndex = 1;
				pane.OnRequestRedraw(s, null);
				//s.OnRequestFocus(s, new ComponentEventArgs(s2));
				System.Diagnostics.Debug.Write("One");
			};

			s2.Action += (object sender, EventArgs e) => {
				pane.SelectedIndex = 0;
				pane.OnRequestRedraw(s2, null);
				//s2.OnRequestFocus(s2, new ComponentEventArgs(s));
				System.Diagnostics.Debug.Write("Two");
			};

			inputs ["three"] = s;

			flow.Add (new Label () { Text = "Hello World"}, s2);

			pane.Add (inputs, flow);
			root.Add (flow);

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
