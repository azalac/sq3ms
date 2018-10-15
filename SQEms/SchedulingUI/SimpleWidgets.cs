using System;

namespace SchedulingUI
{
	public class Box : Component
	{
		
		private const int TOP_LEFT = 0x250F,
		TOP_RIGHT = 0x2513,
		BOTTOM_LEFT = 0x2517,
		BOTTOM_RIGHT = 0x251B,
		VERTICAL = 0x2503,
		HORTIZONTAL = 0x2501;

		#region IComponent implementation

		public override void Draw (IConsole buffer)
		{
			buffer.SetCursorPosition (Left, Top);

			for (int x = Left; x < Left + Width; x++) {
				if (x == Left) {
					buffer.PutCharacter (TOP_LEFT);
				} else if (x == Left + Width - 1) {
					buffer.PutCharacter (TOP_RIGHT);
				} else {
					buffer.PutCharacter (HORTIZONTAL);
				}
			}

			for (int y = Top + 1; y < Top + Height - 1; y++) {
				buffer.PutCharacter (Left, y, VERTICAL);
				buffer.PutCharacter (Left + Width - 1, y, VERTICAL);
			}

			Console.SetCursorPosition (Left, Top + Height - 1);

			for (int x = Left; x < Left + Width; x++) {
				if (x == Left) {
					buffer.PutCharacter (BOTTOM_LEFT);
				} else if (x == Left + Width - 1) {
					buffer.PutCharacter (BOTTOM_RIGHT);
				} else {
					buffer.PutCharacter (HORTIZONTAL);
				}
			}
		}

		#endregion

		public override string ToString ()
		{
			return string.Format ("Box[Top={0}, Left={1}, Width={2}, Height={3}]", Top, Left, Width, Height);
		}

	}

	public class GridContainer : Container
	{
		/// <summary>
		/// If this grid container draw and account for the borders between cells.
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

		public override void DoLayout ()
		{
			// calculate the rows and columns
			int columns = Math.Min (CountX, Components.Count);
			int rows = Math.Min(Components.Count / CountX, CountY);

			// find the available space for the cells
			int available_width = Width - (DrawBorders ? columns + 1 : 0);
			int available_height = Height - (DrawBorders ? rows + 1 : 0);

			int offset = DrawBorders ? 1 : 0;

			double width = available_width / columns + offset;
			double height = available_height / rows + offset;

			for (int x = 0; x < columns; x++) {
				for (int y = 0; y < rows; y++) {
					IComponent component = Components[y * columns + x];
					
					component.Top = Top + (int)(height * y) + offset;
					component.Left = Left + (int)(width * x) + offset;

					component.Width = (int)(width * x % 1 + width) - offset;
					component.Height = (int)(height * y % 1 + height) - offset;
					
					if(component is Container)
					{
						(component as Container).DoLayout ();
					}
				}
			}

		}

		#endregion

	}

	public class FlowContainer : GridContainer
	{
		public bool Vertical { get; set; }

		public FlowContainer():
			base()
		{
			ComponentAdded += (object sender, ComponentEventArgs e) => UpdateCount ();
			ComponentRemoved += (object sender, ComponentEventArgs e) => UpdateCount ();
		}

		private void UpdateCount()
		{
			if (Vertical) {
				CountX = 1;
				CountY = Components.Count;
			} else {
				CountX = Components.Count;
				CountY = 1;
			}

			DoLayout ();
		}

	}

	public class RootContainer : Container
	{
		public IConsole Console { get; private set; }

		public RootContainer(IConsole console)
		{
			ComponentAdded += CheckCount;
			ComponentRemoved += CheckCount;

			Console = console;
		}

		private void CheckCount(object sender, ComponentEventArgs e)
		{
			if (Components.Count > 1) {
				System.Diagnostics.Debug.WriteLine ("Warning: RootContainer has more than one child");
			}
		}

		public void Draw()
		{
			Draw (Console);
		}

		#region implemented abstract members of IContainer

		public override void DoLayout ()
		{
			if (Components.Count > 0)
			{
				IComponent root = Components [0];

				root.Top = 0;
				root.Left = 0;
				root.Height = Console.BufferHeight;
				root.Width = Console.BufferWidth;

				if(root is Container)
				{
					(root as Container).DoLayout ();
				}
			}
		}

		#endregion


	}

}

