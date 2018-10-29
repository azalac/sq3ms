
using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;

namespace SchedulingUI
{

	public class TextInput : Label, IFocusable
	{

		#region IFocusable implementation
		public bool HasFocus { get; set; }
		#endregion

		public int SelectIndex { get; set; }
		public int TextLength { get; set; }

		public TextInput(int TextLength = 20)
		{
			Text = "";
			this.TextLength = TextLength;
			KeyPress += HandleKeyPress;
			Background = ConsoleColor.White;
			Foreground = ConsoleColor.Black;
		}

		private void HandleKeyPress(object sender, ConsoleKeyEventArgs args)
		{
			// if this component doesn't have focus, don't do the keypress
			if (!HasFocus)
			{
				return;
			}

			bool needsRedraw = false;
			
			Rectangle pre = GetTextArea ();

			switch (args.Key.Key)
			{
			case ConsoleKey.Backspace:
				Delete ();
				needsRedraw = true;
				break;

			case ConsoleKey.LeftArrow:
				if (SelectIndex > 0) {
					SelectIndex--;
				}
				needsRedraw = true;
				break;

			case ConsoleKey.RightArrow:
				if (SelectIndex < Text.Length) {
					SelectIndex++;
				}
				needsRedraw = true;
				break;
			}

			if(!char.IsControl(args.Key.KeyChar))
			{
				Insert (args.Key.KeyChar);

				needsRedraw = true;
			}



			if(needsRedraw)
			{
				this.OnRequestRedraw (this, new RedrawEventArgs (GetTextArea().Union(pre)));
			}
		}

		private void Delete()
		{
			// if there's a character to delete, delete it
			if (Text.Length > 0 && SelectIndex > 0)
			{
				StringBuilder sb = new StringBuilder (Text);

				sb.Remove (SelectIndex - 1, 1);

				Text = sb.ToString ();

				SelectIndex--;
			}
		}

		private void Insert(char c)
		{
			// if there's room to insert the character, insert it
			if (Text.Length + 1 < TextLength)
			{
				StringBuilder sb = new StringBuilder (Text);

				sb.Insert (SelectIndex, c);

				Text = sb.ToString ();

				SelectIndex++;
			}
		}

		/// <summary>
		/// This doesn't work
		/// </summary>
		public void OptimizedDraw(IConsole buffer)
		{
			// if there's no width, do nothing
			if (Width <= 0 || Height <= 0) {
				return;
			}

			Tuple<int, int> pos = GetCharPos (0);

			if (pos == null || pos.Item2 > Height)
			{
				return;
			}

			buffer.Foreground = HasFocus ? Background : Foreground;
			buffer.Background = HasFocus ? Foreground : Background;

			//put the string pre-selection
			buffer.PutString (pos.Item1 + Left, pos.Item2 + Top, Text, SelectIndex);
			
			if(SelectIndex < Text.Length)
			{
				pos = GetCharPos (SelectIndex);
			
				if (pos == null || pos.Item2 > Height)
				{
					return;
				}

				buffer.Foreground = !HasFocus ? Background : Foreground;
				buffer.Background = !HasFocus ? Foreground : Background;

				//put the selection
				buffer.PutCharacter (pos.Item1 + Left, pos.Item2 + Top, Text [SelectIndex]);
			}
			
			pos = GetCharPos (SelectIndex + 1);

			if (pos == null || pos.Item2 > Height) {
				return;
			}

			buffer.Foreground = HasFocus ? Background : Foreground;
			buffer.Background = HasFocus ? Foreground : Background;

			if (SelectIndex + 1 < Text.Length)
			{
				//put the string post-selection
				buffer.PutString (pos.Item1 + Left, pos.Item2 + Top, Text.Substring (SelectIndex + 1),
				                  Text.Length - SelectIndex - 1);
			}

			buffer.PutString (pos.Item1 + Left + SelectIndex, pos.Item2 + Top,
			                  new string (' ', Text.Length - SelectIndex));

			
		}

		/// <summary>
		/// Draws the text input (works, but has glitching)
		/// </summary>
		public override void Draw(IConsole buffer)
		{
			// if there's no width, do nothing
			if (Width <= 0 || Height <= 0) {
				return;
			}

			int i = 0;

			// draw the text
			for (; i < Text.Length; i++)
			{
				Tuple<int, int> pos = GetCharPos (i);

				if (pos == null || pos.Item2 > Height)
				{
					break;
				}

				// swap the colors, but only if this component has focus
				bool swap_colors = HasFocus && i != SelectIndex;

				buffer.Foreground = swap_colors ? Background : Foreground;
				buffer.Background = swap_colors ? Foreground : Background;

				buffer.PutCharacter (pos.Item1 + Left, pos.Item2 + Top, Text [i]);
			}

			// draw the whitespace after
			for (; i < TextLength; i++)
			{
				Tuple<int, int> pos = GetCharPos (i);

				if (pos == null || pos.Item2 > Height)
				{
					break;
				}

				bool swap_colors = HasFocus && i != SelectIndex;

				buffer.Foreground = swap_colors ? Background : Foreground;
				buffer.Background = swap_colors ? Foreground : Background;

				buffer.PutCharacter (pos.Item1 + Left, pos.Item2 + Top, ' ');
			}

		}
	}

	public class InputArea : Container
	{
		private string[] fields;

		private Label[] labels;

		private TextInput[] inputs;

		private int selectedIndex = 0;

		public int SelectedIndex
		{
			get
			{
				return selectedIndex;
			}

			set
			{
				inputs [selectedIndex].HasFocus = false;
				OnRequestRedraw (this, new RedrawEventArgs (inputs[selectedIndex]));
				selectedIndex = value;
				inputs [selectedIndex].HasFocus = true;
				OnRequestRedraw (this, new RedrawEventArgs (inputs[selectedIndex]));
			}
		}

		/// <summary>
		/// The width of labels, or 0 for automatic.
		/// </summary>
		public int LabelWidth { get; set; }

		/// <summary>
		/// The width of inputs, or 0 for automatic.
		/// </summary>
		public int InputWidth { get; set; }

		/// <summary>
		/// The height of each row.
		/// </summary>
		public int RowHeight { get; set; }

		public InputArea(params string[] fields)
		{
			this.fields = fields;

			Background = ConsoleColor.White;
			Foreground = ConsoleColor.Black;

			labels = new Label[fields.Length];
			inputs = new TextInput[fields.Length];

			for (int i = 0; i < fields.Length; i++)
			{
				labels [i] = new Label () {
					Text = fields[i],
					ZIndex = 2 * i
				};

				inputs [i] = new TextInput () {
					ZIndex = 2 * i + 1
				};
			}

			KeyPress += HandleKeyPress;

			Add (labels);
			Add (inputs);

			// needed to make focus valid
			SelectedIndex = 0;
		}

		public string this[string field]
		{
			get
			{
				if (Array.Exists (fields, field.Equals))
				{
					return inputs [Array.IndexOf (fields, fields)].Text;
				}
				else
				{
					return null;
				}
			}
		}

		private void HandleKeyPress(object sender, ConsoleKeyEventArgs args)
		{
			if (args.Key.Key == ConsoleKey.UpArrow)
			{
				SelectedIndex = Mod(SelectedIndex - 1, fields.Length);

			}
			else if (args.Key.Key == ConsoleKey.DownArrow)
			{
				SelectedIndex = Mod(SelectedIndex + 1, fields.Length);

			}
		}

		private int CalculateLabelWidth()
		{
			int w = 0;

			if (LabelWidth == 0)
			{
				for (int i = 0; i < labels.Length; i++)
				{
					w = Math.Max (w, labels [i].Text.Length);
				}
			}
			else
			{
				w = LabelWidth;
			}
			return w;
		}

		private static int Mod(int a, int n)
		{
			return (a % n + n) % n;
		}

		#region implemented abstract members of Container

		public override void DoLayout ()
		{
			int i = 0;

			int lw = CalculateLabelWidth ();

			int height = Math.Max (1, RowHeight);

			for (; i < fields.Length; i++)
			{
				Label l = labels [i];
				l.Top = i * height + Top;
				l.Left = Left;
				l.Width = lw;
				l.Height = height;

				TextInput t = inputs [i];
				t.Top = i * height + Top;
				t.Left = lw + 1 + Left;
				t.Width = InputWidth == 0 ? Width - lw - 1 : InputWidth;
				t.TextLength = t.Width;
				t.Height = height;
			}
		}

		#endregion

	}

}
