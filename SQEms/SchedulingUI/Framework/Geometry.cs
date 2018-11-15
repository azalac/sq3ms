using System;
using System.Collections.Generic;
using System.Text;

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
		
		public Rectangle(IComponent component, int offset):
			this (component.Left - offset, component.Top - offset,
			      component.Width + offset * 2, component.Height + offset * 2)
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

		public Rectangle Offset(int amount)
		{
			return new Rectangle (Left - amount, Top - amount,
			                     Width + 2 * amount, Height + 2 * amount);
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

	public class LineHelper
	{

		public const uint LEFT = 0x1;
		public const uint RIGHT = 0x2;
		public const uint TOP = 0x4;
		public const uint BOTTOM = 0x8;

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

		public static void PutLineVertical(uint[,] buffer, int x1, int y1, int length)
		{
			if (x1 < 0 || x1 >= buffer.GetLength (0))
			{
				return;
			}

			for (int i = 0; i < length; i++)
			{
				int y = y1 + i;
				if (y >= 0 && y < buffer.GetLength (1))
				{
					buffer [x1, y] = (i > 0 ? BOTTOM : 0) | (i < length - 1 ? TOP : 0);
				}
			}
		}
		
		public static void PutLineHorizontal(uint[,] buffer, int x1, int y1, int length)
		{
			if (y1 < 0 || y1 >= buffer.GetLength (1))
			{
				return;
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

	public class LineDrawer
	{
		private static readonly LineDrawer GLOBAL = new LineDrawer();

		private uint[,] CharBuffer
		{
			get
			{
				return parent != null ? parent._charbuffer : _charbuffer;
			}

			set
			{
				uint[,] buf = value;

				if (parent != null)
				{
					parent._charbuffer = buf;
				}
				else
				{
					_charbuffer = buf;
				}
			}
		}

        private bool bufferDirty = false;

		private uint[,] _charbuffer;

		private LineDrawer parent;

		public const uint TOP = 0x1;
		public const uint BOTTOM = 0x2;
		public const uint LEFT = 0x4;
		public const uint RIGHT = 0x8;

		public const uint ALL = TOP | BOTTOM | LEFT | RIGHT;

		private Dictionary<IComponent, uint> component_borders =
			new Dictionary<IComponent, uint> ();

		private Dictionary<int, Tuple<Rectangle, uint>> rect_borders =
			new Dictionary<int, Tuple<Rectangle, uint>> ();

		public static LineDrawer FromGlobal()
		{

			LineDrawer drawer = new LineDrawer ();
			drawer.parent = GLOBAL;

			return drawer;
		}

		public void Set(IComponent component, uint border)
		{
			component_borders [component] = border;
		}

		public void Set(int id, Rectangle rect, uint border)
		{
			rect_borders [id] = new Tuple<Rectangle, uint>(rect, border);
		}

		public void Revalidate(IConsole buffer)
		{
			if (CharBuffer == null || 
				CharBuffer.GetLength (0) != buffer.BufferWidth || 
				CharBuffer.GetLength (1) != buffer.BufferHeight) {
				CharBuffer = new uint[buffer.BufferWidth, buffer.BufferHeight];
			}

			foreach (IComponent c in component_borders.Keys)
			{
				PutRectangle (new Rectangle (c, 1), component_borders [c]);
			}

			foreach (Tuple<Rectangle, uint> t in rect_borders.Values)
			{
				PutRectangle(t.Item1, t.Item2);
			}

		}

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
            else
            {
                parent.bufferDirty = true;
            }

		}

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

        public static void GlobalDraw(IConsole buffer)
        {
            GLOBAL.Draw(buffer);
        }

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

