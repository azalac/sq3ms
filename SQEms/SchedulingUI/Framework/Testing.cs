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

			InputArea inputs = new InputArea ("one", "two", " ")
			{
				RowHeight = 2,
				InputWidth = 20
			};

			Button s = new Button ()
			{
				Text = "Submit"
			};

            Button s2 = new Button()
            {
                Text = "Go Back",
				Center = true
            };

            GridContainer flow = new GridContainer ()
			{
				CountY = 3,
				Background = ColorCategory.HIGHLIGHT_BG
			};

            Label label = new Label()
            {
                Text = "Hello World",
				Center = true,
				Foreground = ColorCategory.ERROR_FG
            };

			Label label2 = new Label () {
				Text = "Also Hello World",
				Center = true,
				Foreground = ColorCategory.ERROR_FG
			};
			
			flow.Add (label, label2, s2);

			pane.Add (inputs, flow);

			root.Add (pane);

			s.Action += (object sender, ComponentEventArgs e) => {
                label.Text = string.Format("The first input is: {0}", (inputs["one"] as TextInput).Text);
				label2.Text = string.Format("The second input is: {0}", (inputs["two"] as TextInput).Text);
				s.OnRequestFocus(s, new ComponentEventArgs(s2));
				pane.SetSelectedIndex(1);
			};

            s2.Action += (object sender, ComponentEventArgs e) => {
                s2.OnRequestFocus(s2, new ComponentEventArgs(s));
				pane.SetSelectedIndex(0);
            };

            inputs [" "] = s;

			root.DoLayout ();
			root.Draw ();

			inputs.SelectedIndex = 0;

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
