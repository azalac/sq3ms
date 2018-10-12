using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulingUI
{
    public static class UITesting
    {
		public const int TOP_LEFT = 0x250F,
		TOP_RIGHT = 0x2513,
		BOTTOM_LEFT = 0x2517,
		BOTTOM_RIGHT = 0x251B,
		VERTICAL = 0x2503,
		HORTIZONTAL = 0x2501,
		EMPTY = -1;

		public const int UP = 0x1, DOWN = 0x2, LEFT = 0x4, RIGHT = 0x8;

        public static void test_box()
        {
			draw_box (0, 0, Console.BufferWidth, Console.BufferHeight);
        }

		public static void draw_box(int left, int top, int width, int height)
		{
			for (int x = left; x < left + width; x++) {
				if (x == left) {
					Console.Write (char.ConvertFromUtf32(TOP_LEFT));
				} else if (x == left + width - 1) {
					Console.Write (char.ConvertFromUtf32(TOP_RIGHT));
				} else {
					Console.Write (char.ConvertFromUtf32(HORTIZONTAL));
				}
			}

			for (int y = top + 1; y < top + height - 1; y++) {
				Console.SetCursorPosition (left, y);
				Console.Write (char.ConvertFromUtf32 (VERTICAL));
				Console.SetCursorPosition (left + width - 1, y);
				Console.Write (char.ConvertFromUtf32 (VERTICAL));
			}

			Console.SetCursorPosition (left, top + height - 1);

			for (int x = left; x < left + width; x++) {
				if (x == left) {
					Console.Write (char.ConvertFromUtf32(BOTTOM_LEFT));
				} else if (x == left + width - 1) {
					Console.Write (char.ConvertFromUtf32(BOTTOM_RIGHT));
				} else {
					Console.Write (char.ConvertFromUtf32(HORTIZONTAL));
				}
			}
		}

		public static int char_for_code(int code)
		{
			switch (code) {
			case UP | RIGHT:
				return BOTTOM_LEFT;
			case UP | LEFT:
				return BOTTOM_RIGHT;
			case RIGHT | DOWN:
				return TOP_LEFT;
			case LEFT | DOWN:
				return TOP_RIGHT;
			case UP | DOWN:
				return VERTICAL;
			case LEFT | RIGHT:
				return HORTIZONTAL;
			default:
				return EMPTY;
			}
		}

    }
}
