using System;

namespace SchedulingUI
{
    public class Box : Component
    {

        // Unicode Characters
        private const int UTOP_LEFT = 0x250F,
        UTOP_RIGHT = 0x2513,
        UBOTTOM_LEFT = 0x2517,
        UBOTTOM_RIGHT = 0x251B,
        UVERTICAL = 0x2503,
        UHORIZONTAL = 0x2501;

		// Ascii Characters
        private const int ACORNER = '+',
            AVERTICAL = '|',
            AHORIZONTAL = '-';

        #region IComponent implementation

        public override void Draw(IConsole buffer)
        {
			// true if the console supports complex multi-byte characters
			bool cplx = buffer.SupportsComplex;

			// determine which characters to use
			int TOP_LEFT = cplx ? UTOP_LEFT : ACORNER;
			int TOP_RIGHT = cplx ? UTOP_RIGHT : ACORNER;
			int BOTTOM_LEFT = cplx ? UBOTTOM_LEFT : ACORNER;
			int BOTTOM_RIGHT = cplx ? UBOTTOM_RIGHT : ACORNER;
			int VERTICAL = cplx ? UVERTICAL : AVERTICAL;
			int HORIZONTAL = cplx ? UHORIZONTAL : AHORIZONTAL;

            buffer.SetCursorPosition(Left, Top);

            for (int x = Left; x < Left + Width; x++)
            {
                if (x == Left)
                {
                    buffer.PutCharacter(TOP_LEFT);
                }
                else if (x == Left + Width - 1)
                {
                    buffer.PutCharacter(TOP_RIGHT);
                }
                else
                {
                    buffer.PutCharacter(HORIZONTAL);
                }
            }

            for (int y = Top + 1; y < Top + Height - 1; y++)
            {
                buffer.PutCharacter(Left, y, VERTICAL);
                buffer.PutCharacter(Left + Width - 1, y, VERTICAL);
            }

            Console.SetCursorPosition(Left, Top + Height - 1);

            for (int x = Left; x < Left + Width; x++)
            {
                if (x == Left)
                {
                    buffer.PutCharacter(BOTTOM_LEFT);
                }
                else if (x == Left + Width - 1)
                {
                    buffer.PutCharacter(BOTTOM_RIGHT);
                }
                else
                {
                    buffer.PutCharacter(HORIZONTAL);
                }
            }
        }

        #endregion

        public override string ToString()
        {
            return string.Format("Box[Top={0}, Left={1}, Width={2}, Height={3}]", Top, Left, Width, Height);
        }

    }

	public class Label : Component
	{
		public string Text { get; set; }
		public bool Center { get; set; }

		public bool DoWrapping { get; set; }

		#region implemented abstract members of Component

		public override void Draw (IConsole buffer)
		{
			// if there's no width, or there's no text, do nothing
			if (Width <= 0 || Text.Length == 0) {
				return;
			}

			for (int i = 0; i < Text.Length; i++)
			{
				// if there's no more room for the (non-wrapped) text
				if (i >= Width && !DoWrapping)
				{
					break;
				}

				int x_offset = 0;

				// if the text should be centered, and there's room, determine the offset
				if (Center && Text.Length < Width)
				{
					x_offset = Width / 2 - Text.Length / 2;
				}

				int x = i % Width + x_offset;
				int y = i / Width;

				// if there's no more room for the text
				if (y > Height)
				{
					break;
				}

				buffer.PutCharacter (x + Left, y + Top, Text [i]);
			}
		}

		#endregion


	}

    public class GridContainer : Container
    {
        /// <summary>
        /// If this grid container draws and accounts for the borders between cells.
        /// </summary>
        public bool DrawBorders { get; set; }

        /// <summary>
        /// The number of cells in the x direction.
        /// </summary>
        public int CountX { get; set; }

        /// <summary>
        /// The maximum number of cells in the y direction.
        /// </summary>
        public int CountY { get; set; }

        public GridContainer()
        {
            CountX = 1;
            CountY = 1;
        }

        #region implemented abstract members of IContainer

        public override void DoLayout()
        {
            // calculate the rows and columns
            int columns = Math.Min(CountX, Components.Count);
            int rows = Math.Min(Components.Count / CountX, CountY);

            // find the available space for the cells
            double available_width = Width - (DrawBorders ? columns + 1 : 0);
            double available_height = Height - (DrawBorders ? rows + 1 : 0);

            int offset = DrawBorders ? 1 : 0;

            double width = available_width / columns + offset;
            double height = available_height / rows + offset;

            for (int x = 0; x < columns; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    IComponent component = Components[y * columns + x];

                    component.Top = Top + (int)Math.Round(height * y) + offset;
                    component.Left = Left + (int)Math.Round(width * x) + offset;

                    component.Width = (int)Math.Round(width * x % 1 + width) - offset;
                    component.Height = (int)Math.Round(height * y % 1 + height) - offset;

                    if (component is Container)
                    {
                        (component as Container).DoLayout();
                    }
                }
            }

        }

        #endregion

    }

    public class FlowContainer : Container
    {
        public bool Vertical { get; set; }

		#region implemented abstract members of Container

		public override void DoLayout ()
		{
			if (Vertical)
			{
				LayoutVertical ();
			}
			else
			{
				LayoutHorizontal ();
			}
		}

		private void LayoutVertical()
		{
			double[] heights = new double[Components.Count];
			double sum = 0;

			for (int i = 0; i < Components.Count; i++)
			{
				heights [i] = Components [i].PreferredHeight;
				sum += heights [i];

				System.Diagnostics.Debug.WriteLineIf (Components [i].PreferredHeight == 0,
				                                     "Warning, Component " + i + " has no height");
			}

			int current_height = 0;

			for (int i = 0; i < Components.Count; i++)
			{
				IComponent comp = Components [i];

				comp.Top = current_height;
				comp.Left = 0;

				comp.Width = Width;
				comp.Height = (int)(Height * heights [i] / sum);

				current_height += comp.Height;
			}
		}

		private void LayoutHorizontal()
		{
			double[] widths = new double[Components.Count];
			double sum = 0;

			for (int i = 0; i < Components.Count; i++)
			{
				widths [i] = Components [i].PreferredWidth;
				sum += widths [i];
				
				System.Diagnostics.Debug.WriteLineIf (Components [i].PreferredWidth == 0,
				                                      "Warning, Component " + i + " has no width");
			}

			int current_width = 0;

			for (int i = 0; i < Components.Count; i++)
			{
				IComponent comp = Components [i];

				comp.Top = 0;
				comp.Left = current_width;

				comp.Width = (int)(Width * widths [i] / sum);
				comp.Height = Height;

				current_width += comp.Width;
			}
		}

		#endregion
    }

    public class RootContainer : Container
    {
        public IConsole Console { get; private set; }

		public event EventHandler<ConsoleKeyEventArgs> KeyPress;

        public RootContainer(IConsole console)
        {
            ComponentAdded += CheckCount;
            ComponentRemoved += CheckCount;

			this.RequestRedraw += this.Redraw;

            Console = console;
        }

        private void CheckCount(object sender, ComponentEventArgs e)
        {
            if (Components.Count > 1)
            {
                System.Diagnostics.Debug.WriteLine("Warning: RootContainer has more than one child");
            }
        }

		private void Redraw(object sender, ComponentEventArgs e)
		{
			// if there's a component to redraw, redraw it
			// otherwise redraw the entire buffer
			if (e != null && e.Component != null)
			{
				Draw (e.Component);
			}
			else
			{
				Draw();
			}
		}

        public void Draw()
        {
            Draw(Console);
        }

		public void Draw(int Left, int Top, int Width, int Height)
		{
			Draw (Console.CreateSubconsole (Left, Top, Width, Height));
		}

		public void Draw(IComponent component)
		{
			Draw (component.Left, component.Top, component.Width, component.Height);
		}

		public void OnKeyPress(object sender, ConsoleKeyInfo key)
		{
			KeyPress (sender, new ConsoleKeyEventArgs (key));
		}

        #region implemented abstract members of IContainer

        public override void DoLayout()
        {
            if (Components.Count > 0)
            {
                IComponent root = Components[0];

                root.Top = 0;
                root.Left = 0;
                root.Height = Console.BufferHeight;
                root.Width = Console.BufferWidth;

                if (root is Container)
                {
                    (root as Container).DoLayout();
                }
            }
        }

        #endregion


    }

	public class ConsoleKeyEventArgs : EventArgs
	{
		public ConsoleKeyInfo Key { get; private set; }

		public ConsoleKeyEventArgs(ConsoleKeyInfo key)
		{
			Key = key;
		}
	}

}

