using System;
using System.Collections.Generic;

namespace SchedulingUI
{
	public struct Rectangle
	{
		public int Left, Top, Width, Height;

		public Rectangle(int Left = 0, int Top = 0, int Width = 0, int Height = 0)
		{
			this.Left = Left;
			this.Top = Top;
			this.Width = Width;
			this.Height = Height;
		}

		public Rectangle(IComponent component):
			this (component.Left, component.Top, component.Width, component.Height)
		{
		}

		public override string ToString ()
		{
			return string.Format ("[Rectangle: {0}, {1}, {2}, {3}]", Left, Top, Width, Height);
		}

		public Rectangle Union(Rectangle other)
		{
			List<int> xs = new List<int> ();
			List<int> ys = new List<int> ();

			xs.Add (Left);
			xs.Add (Left + Width);
			xs.Add (other.Left);
			xs.Add (other.Left + other.Width);

			ys.Add (Top);
			ys.Add (Top + Height);
			ys.Add (other.Top);
			ys.Add (other.Top + other.Height);

			xs.Sort ();
			ys.Sort ();

			return BetweenCoords(xs[0], ys[0], xs[3], ys[3]);
		}

		public static Rectangle BetweenCoords(int x1, int y1, int x2, int y2)
		{
			// if x2 is less than x1, swap them
			if (x2 < x1)
			{
				int temp = x1;
				x1 = x2;
				x2 = temp;
			}
			
			// if y2 is less than y1, swap them
			if (y2 < y1)
			{
				int temp = y1;
				y1 = y2;
				y2 = temp;
			}

			return new Rectangle (x1, y1, x2 - x1 + 1, y2 - y1 + 1);
		}

	}
}

