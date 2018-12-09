using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SchedulingUI
{
    /// <summary>
    /// A struct used to represent a 2d area.
    /// </summary>
	public struct Rectangle
	{
        /// <summary>
        /// The dimensions of the rectangle.
        /// </summary>
		public int Left, Top, Width, Height;

		public Rectangle(int Left = 0, int Top = 0, int Width = 0, int Height = 0)
		{
			this.Left = Left;
			this.Top = Top;
			this.Width = Width;
			this.Height = Height;
		}

        /// <summary>
        /// Inherits the component's size once - does not update.
        /// </summary>
        /// <param name="component">The component to inherit from</param>
		public Rectangle(IComponent component):
			this (component.Left, component.Top, component.Width, component.Height)
		{
		}

        /// <summary>
        /// Inherits the component's size once - does not update.
        /// 
        /// Also offsets the size outwards if offset is positive, or inwards if
        /// the offset is negative.
        /// </summary>
        /// <param name="component">The component to inherit from</param>
        /// <param name="offset">The offset</param>
        public Rectangle(IComponent component, int offset):
			this (component.Left - offset, component.Top - offset,
			      component.Width + offset * 2, component.Height + offset * 2)
		{
		}

		public override string ToString ()
		{
			return string.Format ("[Rectangle: {0}, {1}, {2}, {3}]", Left, Top, Width, Height);
		}

        /// <summary>
        /// Creates a rectangle which encompasses this rectangle and another rectangle.
        /// </summary>
        /// <param name="other">The other rectangle</param>
        /// <returns>The encompassing rectangle</returns>
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

        /// <summary>
        /// Creates a rectangle which is offset (inwards or outwards) by the
        /// specified amount.
        /// </summary>
        /// <param name="amount">The amount to offset</param>
        /// <returns>The new rectangle</returns>
		public Rectangle Offset(int amount)
		{
			return new Rectangle (Left - amount, Top - amount,
			                     Width + 2 * amount, Height + 2 * amount);
		}

        /// <summary>
        /// Creates the smallest rectangle which has the two coordinates as
        /// corners.
        /// </summary>
        /// <param name="x1">The first corner's X</param>
        /// <param name="y1">The first corner's Y</param>
        /// <param name="x2">The second corner's X</param>
        /// <param name="y2">The second corner's Y</param>
        /// <returns>The rectangle</returns>
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

        public static Rectangle Encompassing(IEnumerable<Tuple<int, int>> points)
        {
            List<int> xs = new List<int>();
            List<int> ys = new List<int>();

            foreach (Tuple<int, int> point in points)
            {
                xs.Add(point.Item1);
                ys.Add(point.Item2);
            }

            if (xs.Count == 0)
            {
                return new Rectangle();
            }

            xs.Sort();
            ys.Sort();

            return BetweenCoords(xs.First(), ys.First(), xs.Last(), ys.Last());
        }

    }

    /// <summary>
    /// A class which helps with drawing lines and modifying the line buffer.
    /// </summary>
	public static class LineHelper
	{
		private const uint LEFT = 0x1;
		private const uint RIGHT = 0x2;
		private const uint TOP = 0x4;
		private const uint BOTTOM = 0x8;

        /// <summary>
        /// A dictionary which (probably) contains all possible combinations of
        /// LEFT, RIGHT, TOP, and BOTTOM and their corresponding characters.
        /// </summary>
		private static Dictionary<uint, short> UnicodeChars = new Dictionary<uint, short>();

		static LineHelper()
		{
			UnicodeChars [0] = (short)' ';

			// lines
			UnicodeChars [LEFT | RIGHT] = 0x2501;
			UnicodeChars [TOP | BOTTOM] = 0x2503;
			UnicodeChars [LEFT] = UnicodeChars [RIGHT] = UnicodeChars [LEFT | RIGHT];
			UnicodeChars [TOP] = UnicodeChars [BOTTOM] = UnicodeChars [TOP | BOTTOM];

			// corners
			UnicodeChars [RIGHT | BOTTOM] = 0x250F;
			UnicodeChars [LEFT | BOTTOM] = 0x2513;
			UnicodeChars [RIGHT | TOP] = 0x2517;
			UnicodeChars [LEFT | TOP] = 0x251B;

			// tees
			UnicodeChars [LEFT | BOTTOM | RIGHT] = 0x2523;
			UnicodeChars [LEFT | TOP | RIGHT] = 0x252B;
			UnicodeChars [TOP | RIGHT | BOTTOM] = 0x2533;
			UnicodeChars [TOP | LEFT | BOTTOM] = 0x253B;

			// cross
			UnicodeChars [TOP | LEFT | BOTTOM | RIGHT] = 0x254B;
		}

        /// <summary>
        /// Gets the UTF-16 character (as a short) for a given code
        /// </summary>
        /// <param name="c">The code</param>
        /// <returns>The character</returns>
		public static short GetUnicodeChar(uint c)
		{
			bool debug = false;

			if (debug)
			{
				return (short)c.ToString ("X") [0];
			}
			else
			{
				return UnicodeChars [c];
			}
		}

        /// <summary>
        /// Gets the (UTF-16 encoded) ascii character for a given code
        /// </summary>
        /// <param name="c">The code</param>
        /// <returns>The character</returns>
		public static short GetAsciiChar(uint c)
		{
            switch (c)
            {
                case 0:
                    return (short)' ';
                case LEFT | RIGHT:
                case LEFT:
                case RIGHT:
                    return (short)'-';
                case TOP | BOTTOM:
                case TOP:
                case BOTTOM:
                    return (short)'|';
                default:
                    return (short)'+';
            }
		}

        /// <summary>
        /// Puts a vertical line in the buffer at the specified position with
        /// the requested length.
        /// </summary>
        /// <param name="buffer">The buffer</param>
        /// <param name="x1">The X coord (Left)</param>
        /// <param name="y1">The Y coord (Top)</param>
        /// <param name="length">The length of the line</param>
		public static void PutLineVertical(uint[,] buffer, int x1, int y1, int length)
		{
			if (x1 < 0 || x1 >= buffer.GetLength (0))
			{
				return;
			}

            // if the length is negative, offset the start by the length and
            // make the length positive.
            if (length < 0)
            {
                length *= -1;
                y1 -= length;
            }

			for (int i = 0; i < length; i++)
			{
				int y = y1 + i;
				if (y >= 0 && y < buffer.GetLength (1))
				{
                    // only draw the bottom 
					buffer [x1, y] = (i > 0 ? BOTTOM : 0) | (i < length - 1 ? TOP : 0);
				}
			}
		}

        /// <summary>
        /// Puts a horizontal line in the buffer at the specified position with
        /// the requested length.
        /// </summary>
        /// <param name="buffer">The buffer</param>
        /// <param name="x1">The X coord (Left)</param>
        /// <param name="y1">The Y coord (Top)</param>
        /// <param name="length">The length of the line</param>
        public static void PutLineHorizontal(uint[,] buffer, int x1, int y1, int length)
		{
			if (y1 < 0 || y1 >= buffer.GetLength (1))
			{
				return;
			}
            
            // if the length is negative, offset the start by the length and
            // make the length positive.
            if (length < 0)
            {
                length *= -1;
                x1 -= length;
            }

            for (int i = 0; i < length; i++)
			{
				int x = x1 + i;
				if (x >= 0 && x < buffer.GetLength (0))
				{
					buffer [x, y1] = (i > 0 ? LEFT : 0) | (i < length - 1 ? RIGHT : 0);
				}
			}
		}

	}

    /// <summary>
    /// A class which allows components to draw lines. Components should create
    /// the drawer with <see cref="FromGlobal"/> in order to mesh the lines
    /// together, but it is not required.
    /// </summary>
	public class LineDrawer
	{
		private static readonly LineDrawer GLOBAL = new LineDrawer();

        /// <summary>
        /// A property to help with accessing the proper buffer.
        /// </summary>
		private uint[,] CharBuffer
		{
			get
			{
				return parent != null ? parent.CharBuffer : _charbuffer;
			}

			set
			{
				uint[,] buf = value;

				if (parent != null)
				{
					parent.CharBuffer = buf;
				}
				else
				{
					_charbuffer = buf;
				}
			}
		}

        /// <summary>
        /// Whether the buffer needs to be redraw or not.
        /// Only used when this drawer is a parent.
        /// </summary>
        private bool bufferDirty = false;
        
        /// <summary>
        /// The actual buffer.
        /// Only used when this drawer has no parent.
        /// </summary>
		private uint[,] _charbuffer;

        /// <summary>
        /// This line drawer's parent.
        /// </summary>
		private LineDrawer parent;

        /// <summary>
        /// The top border.
        /// </summary>
		public const uint TOP = 0x1;

        /// <summary>
        /// The bottom border.
        /// </summary>
		public const uint BOTTOM = 0x2;

        /// <summary>
        /// The left border.
        /// </summary>
		public const uint LEFT = 0x4;
        
        /// <summary>
        /// The right border.
        /// </summary>
		public const uint RIGHT = 0x8;

        /// <summary>
        /// All borders.
        /// </summary>
		public const uint ALL = TOP | BOTTOM | LEFT | RIGHT;

		private Dictionary<IComponent, uint> component_borders =
			new Dictionary<IComponent, uint> ();

		private Dictionary<int, Tuple<Rectangle, uint>> rect_borders =
			new Dictionary<int, Tuple<Rectangle, uint>> ();

        /// <summary>
        /// Creates a line drawer which draws to the global buffer.
        /// Allows lines to mesh together nicely.
        /// </summary>
        /// <returns></returns>
		public static LineDrawer FromGlobal()
		{

			LineDrawer drawer = new LineDrawer ();
			drawer.parent = GLOBAL;

			return drawer;
		}

        /// <summary>
        /// Sets a component's border
        /// </summary>
        /// <param name="component">The component</param>
        /// <param name="border">The componen'ts border</param>
		public void Set(IComponent component, uint border)
		{
            lock (component_borders)
            {
                component_borders[component] = border;
            }
		}

        /// <summary>
        /// Draws an arbitrary rectangle.
        /// </summary>
        /// <param name="id">The rectangle's ID (rectangles are value types)</param>
        /// <param name="rect">The rectangle to draw</param>
        /// <param name="border">The rectangle's border</param>
		public void Set(int id, Rectangle rect, uint border)
        {
            lock (rect_borders)
            {
                rect_borders[id] = new Tuple<Rectangle, uint>(rect, border);
            }
		}

        /// <summary>
        /// Revalidates the buffer, and redraws the components and rectangles.
        /// </summary>
        /// <param name="buffer">The buffer to check the width against</param>
		public void Revalidate(IConsole buffer)
		{
			if (CharBuffer == null || 
				CharBuffer.GetLength (0) != buffer.BufferWidth || 
				CharBuffer.GetLength (1) != buffer.BufferHeight) {
				CharBuffer = new uint[buffer.BufferWidth, buffer.BufferHeight];
			}

            lock (component_borders)
            {
                foreach (IComponent c in component_borders.Keys)
                {
                    PutRectangle(new Rectangle(c, 1), component_borders[c]);
                }
            }

            lock (rect_borders)
            {
                foreach (Tuple<Rectangle, uint> t in rect_borders.Values)
                {
                    PutRectangle(t.Item1, t.Item2);
                }
            }

		}

        /// <summary>
        /// Puts a rectangle in the buffer at its location, with the specified
        /// border.
        /// </summary>
        /// <param name="rect">The rectangle</param>
        /// <param name="borders">The rectangle's borders</param>
		private void PutRectangle(Rectangle rect, uint borders)
		{
			if ((borders & TOP) != 0)
			{
				LineHelper.PutLineHorizontal (CharBuffer, rect.Left, rect.Top, rect.Width + 1);
			}

			if ((borders & BOTTOM) != 0)
			{
				LineHelper.PutLineHorizontal (CharBuffer, rect.Left, rect.Top + rect.Height, rect.Width + 1);
			}

			if ((borders & LEFT) != 0)
			{
				LineHelper.PutLineVertical (CharBuffer, rect.Left, rect.Top, rect.Height + 1);
			}

			if ((borders & RIGHT) != 0)
			{
				LineHelper.PutLineVertical (CharBuffer, rect.Left + rect.Width, rect.Top, rect.Height + 1);
			}
		}

        /// <summary>
        /// Draws 
        /// </summary>
        /// <param name="buffer"></param>
		public void Draw(IConsole buffer)
		{
            if (parent == null && bufferDirty)
            {
                if (CharBuffer == null)
                {
                    Revalidate(buffer);
                }

                if (false)
                {
                    FastDraw(buffer);
                }
                else
                {
                    SlowDraw(buffer);
                }

                bufferDirty = false;
            }
            else if(parent != null)
            {
                parent.bufferDirty = true;
            }

		}

        /// <summary>
        /// This algorithm draws the buffer character by character. Only draws
        /// the characters which are valid (ie != 0).
        /// </summary>
        /// <param name="buffer">The buffer to draw to</param>
		private void SlowDraw(IConsole buffer)
		{
            // optimization because system calls are expensive
            // somehow these were eating more cpu than the putcharacter
            int width = buffer.BufferWidth;
            int height = buffer.BufferHeight;

			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					uint code = CharBuffer [x, y];

					if (code != 0)
					{
						short ch = buffer.SupportsComplex ?
								  LineHelper.GetUnicodeChar(code):
								  LineHelper.GetAsciiChar(code);

						buffer.PutCharacter (x, y, (int)ch);
					}
				}
			}
		}

        /// <summary>
        /// This algorithm isn't working properly - do not use.
        /// This method is supposed to chunk up the buffer, and draw the lines
        /// as strings if possible.
        /// </summary>
		private void FastDraw(IConsole buffer)
		{
            List<short> shortstr = new List<short>();

            int w = buffer.BufferWidth;
            int h = buffer.BufferHeight;

            int prex = 0, prey = 0;

            Func<uint, short> converter = null;

            if(buffer.SupportsComplex)
            {
                converter = LineHelper.GetUnicodeChar;
            }
            else
            {
                converter = LineHelper.GetAsciiChar;
            }

            // chunk the buffer
            for (int i = 0; i < w * h; i++)
            {
                int x = i % w;
                int y = i / w;

                uint code = CharBuffer[x, y];

                if(code == 0)
                {
                    buffer.PutString(prex, prey, ShortsToString(shortstr));
                    shortstr.Clear();
                }
                else
                {
                    if(shortstr.Count == 0)
                    {
                        prex = x;
                        prey = y;
                    }

                    shortstr.Add(converter(code));
                }

            }

		}

        /// <summary>
        /// Draws the global buffer - should only be called by a RootContainer.
        /// </summary>
        /// <param name="buffer">The buffer to draw to</param>
        public static void GlobalDraw(IConsole buffer)
        {
            GLOBAL.Draw(buffer);
        }

        /// <summary>
        /// Resets the global buffer to all zeros.
        /// </summary>
        public static void GlobalReset(IConsole buffer)
        {
            GLOBAL.CharBuffer = new uint[buffer.BufferWidth, buffer.BufferHeight];
        }

        /// <summary>
        /// Converts a list of shorts to a string.
        /// </summary>
        /// <param name="shorts">The shorts to convert</param>
        /// <returns>The string</returns>
		private string ShortsToString(List<short> shorts)
		{
			byte[] b_arr = new byte[shorts.Count * 2];

			for (int i = 0; i < shorts.Count; i++)
			{
				short s = shorts[i];
				b_arr [i * 2] = (byte)s;
				b_arr [i * 2 + 1] = (byte)(s >> 8);
			}

			return new string(Encoding.Unicode.GetChars (b_arr));
		}
	}
}

