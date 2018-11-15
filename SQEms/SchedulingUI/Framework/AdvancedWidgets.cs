
using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Threading;

namespace SchedulingUI
{

	public class TextInput : Label
	{

		public int SelectIndex { get; set; }
		public int TextLength { get; set; }

		public TextInput(int TextLength = 10)
		{
			Text = "";
			this.TextLength = TextLength;
			KeyPress += HandleKeyPress;
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
		/// Draws this text input
		/// </summary>
		public override void Draw(IConsole buffer)
		{
            int xoffset = Center ? (Width - TextLength) / 2 : 0;

            string drawtext = Text + new string(' ', TextLength - Text.Length);

            string pre = SelectIndex > 0 ? drawtext.Substring(0, SelectIndex) : "";
            char sel = drawtext[SelectIndex];
            string post = SelectIndex < drawtext.Length - 1 ? drawtext.Substring(SelectIndex + 1) : "";

            UpdateColors(buffer, 0);
            buffer.PutString(Left + xoffset, Top, pre);

            UpdateColors(buffer, SelectIndex);
            buffer.PutCharacter(Left + SelectIndex + xoffset, Top, sel);

            UpdateColors(buffer, SelectIndex + 1);
            buffer.PutString(Left + SelectIndex + 1 + xoffset, Top, post);
		}

		

        private void UpdateColors(IConsole buffer, int current_index)
        {
            if (current_index == SelectIndex)
            {
                buffer.Foreground = Foreground;
                buffer.Background = ColorCategory.HIGHLIGHT_BG;
                return;
            }

            if (!HasFocus)
            {
                buffer.Foreground = Foreground;
                buffer.Background = Background;
            }
            else
            {
                buffer.Foreground = Background;
                buffer.Background = Foreground;
            }
        }
	}

	public class Button : Label
    {
        /// <summary>
        /// The milliseconds since a button was last activated. Used to
        /// de-bounce buttons, in order to prevent multiple from being pressed
        /// in order when the user didn't want to.
        /// </summary>
        private static double LAST_ACTIVATION = 0;

        /// <summary>
        /// The milliseconds between activations.
        /// </summary>
        private static long ACTIVATION_DELAY = 20;

        /// <summary>
        /// This probably doesn't need its own static variable, but it helps
        /// with code description.
        /// </summary>
        private static DateTime UNIX_EPOCH = new DateTime(1970, 1, 1);
        
        public event EventHandler<ComponentEventArgs> Action;

        public Button()
        {
			this.KeyPress += HandleKeyPress;
			Text = "Button";

        }

        private void HandleKeyPress(object sender, ConsoleKeyEventArgs args)
		{
			if (HasFocus && args.Key.Key == ConsoleKey.Enter)
            {
                double current_millis = (DateTime.UtcNow - UNIX_EPOCH).TotalMilliseconds;

                if (current_millis - LAST_ACTIVATION > ACTIVATION_DELAY)
                {
					if (Action != null)
					{
						Action (sender, new ComponentEventArgs (this));
					}
					else
					{
						System.Diagnostics.Debug.WriteLine ("Warning: button with text '{0}' has no action", Text);
					}

                    LAST_ACTIVATION = current_millis;
                }
			}
		}

		public override void UpdateColors (IConsole buffer)
		{
			buffer.Foreground = HasFocus ? Background : Foreground;
			buffer.Background = HasFocus ? Foreground : Background;
		}

	}

    public class InputArea : Container
	{
		private readonly string[] fields;

		private readonly Label[] labels;

		private readonly IComponent[] components;

		private int selectedIndex = 0;
        
		public int SelectedIndex
		{
			get
			{
				return selectedIndex;
			}

			set
			{
                int old_selected = selectedIndex;
				selectedIndex = value;

                OnRequestFocus(this, new ComponentEventArgs(components[selectedIndex]));
                OnRequestRedraw (this, new RedrawEventArgs(components[old_selected]));
                OnRequestRedraw (this, new RedrawEventArgs (components [selectedIndex]));
			}
		}

		/// <summary>
		/// The width of labels, or 0 for automatic.
		/// </summary>
		public int LabelWidth { get; set; }

		/// <summary>
		/// The width of inputs, or 0 for automatic.
        /// Limits text to value - 1.
		/// </summary>
		public int InputWidth { get; set; }

		/// <summary>
		/// The height of each row.
		/// </summary>
		public int RowHeight { get; set; }
        
		public InputArea(params string[] fields)
		{
			this.fields = fields;

			labels = new Label[fields.Length];
			components = new IComponent[fields.Length];

			for (int i = 0; i < fields.Length; i++)
			{
				labels [i] = new Label () {
					Text = fields[i],
					ZIndex = 2 * i
				};
                
				components [i] = new TextInput () {
					ZIndex = 2 * i + 1
				};
			}

			KeyPress += HandleKeyPress;

			Add (labels);
			Add (components);

        }

		public IComponent this[string field]
		{
			get
			{
				if (Array.Exists (fields, field.Equals))
				{
					return components [Array.IndexOf (fields, field)];
				}
				else
				{
					return null;
				}
			}

			set
			{
				if (Array.Exists (fields, field.Equals))
				{
					int i = Array.IndexOf (fields, field);
					Remove (components [i]);
					components [i] = value;
					Add (components [i]);

					if (i == SelectedIndex)
					{
						SelectedIndex = SelectedIndex;
					}
				}
			}
		}

		private void HandleKeyPress(object sender, ConsoleKeyEventArgs args)
		{
			if (!Visible)
			{
				return;
			}

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

		protected override void DoLayoutImpl ()
		{
			int lw = CalculateLabelWidth ();

			int height = RowHeight < 1 ? 1 : RowHeight;

			for (int i = 0; i < fields.Length; i++)
			{
				Label l = labels [i];
				l.Top = i * height + Top;
				l.Left = Left;
				l.Width = lw;
				l.Height = height;
				
				IComponent t = components [i];
				t.Top = i * height + Top;
				t.Left = lw + 1 + Left;
				t.Width = InputWidth == 0 ? Width - lw - 1 : InputWidth;
				t.Height = height;

                if (t is TextInput)
                {
                    (t as TextInput).TextLength = t.Width;
                }
			}
		}

		#endregion

	}

    /// <summary>
    /// Controls multiple input controls, allowing the user to select between them.
    /// </summary>
    /// <remarks>
    /// This is not a component. It controls the components, but has no visual aspect.
    /// Components that should be controlled by it need to be added to its internal list.
    /// </remarks>
    public class InputController
    {
        public Component Parent {
            get
            {
                return parent;
            }
            set
            {
                if(parent != null)
                {
                    parent.KeyPress -= HandleKeypress;
                }

                parent = value;
                parent.KeyPress += HandleKeypress;
            }
        }

        public bool HasFocus {
            get
            {
                return hasFocus;
            }
            set
            {
                // TODO components need to be unfocussed once this controller is unfocussed
                hasFocus = value;
            }
        }

        public bool Horizontal { get; set; }

        public event EventHandler<ControllerEventArgs> RequestFocus;

        private readonly List<object> Components = new List<object>();

        private bool hasFocus = false;

        private Component parent;

        private int selectedIndex = -1;
        
        private void HandleKeypress(object sender, ConsoleKeyEventArgs args)
        {
            if (!Parent.Visible || !HasFocus)
            {
                return;
            }

            ConsoleKey Dec = Horizontal ? ConsoleKey.LeftArrow : ConsoleKey.UpArrow;
            ConsoleKey Inc = Horizontal ? ConsoleKey.RightArrow : ConsoleKey.DownArrow;

            if (args.Key.Key == Dec)
            {
                SetSelectedIndex(Mod(selectedIndex - 1, Components.Count));
            }
            else if (args.Key.Key == Inc)
            {
                SetSelectedIndex(Mod(selectedIndex + 1, Components.Count));
            }
        }
        
        public void SetSelectedIndex(int newSelection)
        {
            // do nothing if there are no components
            if(Components.Count == 0)
            {
                selectedIndex = -1;
                return;
            }
            
            int oldSelection = selectedIndex;
            selectedIndex = newSelection;

            if (Components[selectedIndex] is IComponent c)
            {
                parent.OnRequestFocus(this, new ComponentEventArgs(c));
                parent.OnRequestRedraw(this, new RedrawEventArgs(c));
            }else if(Components[selectedIndex] is InputController c2)
            {
                c2.HasFocus = true;
            }

            if(oldSelection != -1 && Components[oldSelection] is IComponent)
            {
                parent.OnRequestRedraw(this, new RedrawEventArgs(Components[oldSelection] as IComponent));
            }else if(oldSelection != -1 && Components[oldSelection] is InputController)
            {
                (Components[oldSelection] as InputController).HasFocus = false;
            }

        }

        public void Add(IComponent component)
        {
            Components.Add(component);
        }

        public void Add(InputController controller)
        {
            Components.Add(controller);

            controller.RequestFocus += OnRequestFocus;
        }

        public void OnRequestFocus(object sender, ControllerEventArgs args)
        {
            if (RequestFocus != null)
            {
                RequestFocus(sender, args);
            }
        }

        private static int Mod(int a, int n)
        {
            return (a % n + n) % n;
        }
        
    }

    public class TabbedPane : Container
	{
		public int SelectedIndex { get; private set; }

		public void SetSelectedIndex(int index)
		{
			SelectedIndex = index;
			DoLayout();
			OnRequestRedraw(this, new RedrawEventArgs(this));
		}

		#region implemented abstract members of Container

		protected override void DoLayoutImpl ()
		{
            for (int i = 0; i < Components.Count; i++)
            {
                IComponent icomp = Components[i];

                icomp.Top = Top;
                icomp.Left = Left;
                icomp.Width = Width;
                icomp.Height = Height;

				if (icomp is Component)
				{
					(icomp as Component).Visible = i == SelectedIndex;
				}

            }
		}

		#endregion


	}

    public class ControllerEventArgs : EventArgs
    {
        public InputController Controller { get; private set; }

        public ControllerEventArgs(InputController controller)
        {
            Controller = controller;
        }
    }

}
