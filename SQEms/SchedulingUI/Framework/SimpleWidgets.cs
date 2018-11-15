using System;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

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

		public Label(string Text = "")
		{
            this.Text = Text;
		}
        
        /// <summary>
        /// Gets the relative position for a character's position.
		/// Does not account for the contents of the text.
        /// </summary>
        /// <param name="i">The character's index</param>
        /// <returns>The relative position</returns>
		public Tuple<int, int> GetCharPos(int i)
		{
			// if there's no more room for the (non-wrapped) text
			// or if the index is invalid, return null
			if (i >= Width && !DoWrapping)
			{
				return null;
			}

			// if the text should be centered, and there's room, determine the offset
			int x_offset = 0;
			if (Center && Text.Length < Width)
			{
				x_offset = Width / 2 - Text.Length / 2;
			}


			// calculate the relative position
			int x = i % Width + x_offset;
			int y = i / Width;

			return new Tuple<int, int> (x, y);
		}
			
		/// <summary>
		/// Gets the relative position for every character.
		/// Accounts for the contents of the text.
		/// </summary>
		/// <param name="i">The character's index</param>
		/// <returns>The relative positions for each text (index, x, y)</returns>
		public IEnumerable<Tuple<int, int, int>> GetRealCharPositions()
		{
            
			int x = Center ? GetXOffset(0, Width) : 0;
			int y = 0;

            int i = 0;

			while(i < Text.Length)
			{
				// if there's no more room for the (non-wrapped) text
				// or if the index is invalid, stop iterating
				if (i >= Width && !DoWrapping)
				{
					yield break;
				}

				char c = Text [i];

				Tuple<int, int, int> ret = new Tuple<int, int, int> (i, x, y);

                i++;

                // on newlines, go to the next line and don't send the character
                if(c == '\n')
                {
                    x = Center ? GetXOffset(i, Width) : 0;
                    y++;
                    continue;
                }
                //"FormatName:Direct=TCP:ip\private$\name"

				// on normal characters, go to the next position
				x++;
				
				// if the cursor should go to the next line, update x and increment y
				if (x >= Width)
				{
					x = Center ? GetXOffset(i, Width) : 0;
					y++;
				}
                
				yield return ret;

			}

			yield break;
		}

        public IEnumerable<Tuple<int, int, string>> GetChunks()
        {
            LinkedList<string> lines = new LinkedList<string>(Text.Split('\n'));

            LinkedListNode<string> curr = lines.First;

            while(curr != null)
            {
                while(curr.Value.Length > Width)
                {
                    lines.AddBefore(curr, curr.Value.Substring(0, Width));

                    curr.Value = curr.Value.Substring(Width);
                }

                curr = curr.Next;
            }

            int y = 0;

            curr = lines.First;

            while(curr != null)
            {
                int x = (Width - curr.Value.Length) / 2;
                yield return new Tuple<int, int, string>(x, y, curr.Value);
                y++;
                curr = curr.Next;
            }

            yield break;
        }

        private int GetXOffset(int i, int max_length)
        {
            for(int idelta = 0; idelta < max_length; idelta++)
            {
                // at a newline, or the end, return the offset
                if(i + idelta >= Text.Length - 1 || Text[i + idelta] == '\n')
                {
                    return (max_length - idelta) / 2;
                }
            }

            return 0;
        }

        /// <summary>
        /// Gets the rectangle which covers all text in this label.
		/// The returned rectangle may have no area, if there is no text in this label.
        /// </summary>
        /// <returns>The rectangle.</returns>
		public Rectangle GetTextArea()
		{
			IEnumerable<Tuple<int, int, int>> positions = GetRealCharPositions ();
			
			if (positions.Count() == 0)
			{
				return new Rectangle (Left, Top, 0, 0);
			}

			Tuple<int, int, int> TopLeft = positions.First();
			Tuple<int, int, int> BottomRight = positions.Last();

			return Rectangle.BetweenCoords (TopLeft.Item2 + Left, TopLeft.Item3 + Top,
			                               BottomRight.Item2 + Left, BottomRight.Item3 + Top);
		}

		#region implemented abstract members of Component

		public override void Draw (IConsole buffer)
		{
			// if there's no width, or there's no text, do nothing
			if (Width <= 0 || Text.Length == 0) {
				return;
			}

			foreach (Tuple<int, int, string> chunk in GetChunks())
			{
                buffer.PutString(Left + chunk.Item1, Top + chunk.Item2, chunk.Item3);
			}
		}

		#endregion


	}

    public class GridContainer : Container
    {
		/// <summary>
		/// If this container draws and accounts for the borders between cells.
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

		/// <summary>
		/// The border side flags. Determines the sides to draw.
		/// </summary>
		public uint OuterBorders { get; set; }
        
		private LineDrawer lines = LineDrawer.FromGlobal();

        public GridContainer()
        {
            CountX = 1;
            CountY = 1;
			OuterBorders = LineDrawer.ALL;
        }

        #region implemented abstract members of Container

        protected override void DoLayoutImpl()
        {
            double width_per = ((double)Width) / CountX;
            double height_per = ((double)Height) / CountY;

            for (int xi = 0; xi < CountX; xi++)
            {
                for (int yi = 0; yi < CountY; yi++)
                {
                    int i = yi * CountX + xi;

                    int x = (int)(width_per * xi) + 1,
                        y = (int)(height_per * yi) + 1,
                        w = (int)(width_per),
                        h = (int)(height_per);

                    if (i < Components.Count) {
                        IComponent component = Components[i];

                        component.Left = Left + x;
                        component.Top = Top + y;

                        component.Width = w;
                        component.Height = h;
                    }
                }
            }

            if (DrawBorders)
            {
                double dist = 0;

                int x_lower = ((OuterBorders & LineDrawer.LEFT) != 0) ? 1 : 0;
                int x_upper = (((OuterBorders & LineDrawer.RIGHT) != 0) ? 1 : 0) + CountX;

                int y_lower = ((OuterBorders & LineDrawer.TOP) != 0) ? 1 : 0;
                int y_upper = (((OuterBorders & LineDrawer.BOTTOM) != 0) ? 1 : 0) + CountY;

                for (int xi = x_lower; xi < x_upper; xi++)
                {
                    int x = (int)dist;

                    lines.Set(2 * xi, new Rectangle(Left + x, Top, 0, Height), LineDrawer.LEFT);

                    dist += width_per;
                }

                dist = 0;

                for (int yi = y_lower; yi < y_upper; yi++)
                {
                    int y = (int)dist;

                    lines.Set(2 * yi + 1, new Rectangle(Left, Top + y, Width, 0), LineDrawer.TOP);

                    dist += height_per;
                }
            }
        }

		public override void Draw (IConsole buffer)
		{
			base.Draw (buffer);

			if (DrawBorders)
			{
				lines.Revalidate (buffer);
				lines.Draw (buffer);
			}
		}

        #endregion
        
    }

    public class FlowContainer : Container
	{
		/// <summary>
		/// If this container draws and accounts for the borders between cells.
		/// </summary>
		public bool DrawBorders { get; set; }

        public bool Vertical { get; set; }
		
		private LineDrawer lines = LineDrawer.FromGlobal();

		#region implemented abstract members of Container

		protected override void DoLayoutImpl ()
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
				                                      "Warning, Component " + i + " has no preferred height");
			}

			int current_height = 0;

			for (int i = 0; i < Components.Count; i++)
			{
				int x = Left;
				int y = Top + current_height;
				int w = Width;
				int h = (int)(Height * heights [i] / sum);
				
				current_height += h;

				if (DrawBorders)
				{
					uint code = LineDrawer.LEFT | LineDrawer.TOP | LineDrawer.RIGHT;

					if (i == Components.Count - 1)
					{
						code |= LineDrawer.BOTTOM;
					}

					lines.Set (i, new Rectangle (x, y, w - 1, h - 1), code);
					
					x++;
					y++;
					w -= 2;
					h -= 2;
				}

				IComponent comp = Components [i];

				comp.Left = x;
				comp.Top = y;

				comp.Width = w;
				comp.Height = h;

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
				                                      "Warning, Component " + i + " has no preferred width");
			}

			int current_width = 0;

			for (int i = 0; i < Components.Count; i++)
			{
				
				int x = Left + current_width;
				int y = Top;
				int w = (int)(Width * widths [i] / sum);
				int h = Height;

				current_width += w;

				if (DrawBorders)
				{
					uint code = LineDrawer.LEFT | LineDrawer.TOP | LineDrawer.BOTTOM;

					if (i == Components.Count - 1)
					{
						code |= LineDrawer.RIGHT;
					}

					lines.Set (i, new Rectangle (x, y, w - 1, h - 1), code);

					x++;
					y++;
					w -= 2;
					h -= 2;
				}

				IComponent comp = Components [i];

				comp.Top = y;
				comp.Left = x;

				comp.Width = w + 1;
				comp.Height = h;

            }
        }
		
		public override void Draw (IConsole buffer)
		{
			base.Draw (buffer);

			if (DrawBorders)
			{
				lines.Revalidate (buffer);
				lines.Draw (buffer);
			}
		}

		#endregion
    }

    public class BinaryContainer : Container
    {
        public bool Vertical { get; set; }

        public IComponent First { get; set; }

        public IComponent Second { get; set; }

        protected override void DoLayoutImpl()
        {
            // this container must contain both components
            if(!Components.Contains(First) || !Components.Contains(Second))
            {
                System.Diagnostics.Debug.WriteLine("Warning: BinaryContainer's first & second aren't in the container");
                return;
            }
            
            if(Vertical)
            {
                LayoutVertical(First, Second);
            }
            else
            {
                LayoutHorizontal(First, Second);
            }
        }

        private void LayoutVertical(IComponent first, IComponent second)
        {
            first.Left = Left;
            first.Top = Top;
            first.Width = Width;
            first.Height = Math.Min(Height, first.PreferredHeight);

            second.Left = Left;
            second.Top = Top + first.Height;
            second.Width = Width;
            second.Height = Height - first.Height;
        }

        private void LayoutHorizontal(IComponent first, IComponent second)
        {
            first.Left = Left;
            first.Top = Top;
            first.Width = Math.Min(Width, first.PreferredWidth);
            first.Height = Height;

            second.Left = Left + first.Width;
            second.Top = Top;
            second.Width = Width - first.Width;
            second.Height = Height;
        }
    }

    public enum InterfaceEvent
	{
		REDRAW,
		SET_FOCUS,
        SET_CONTROLLER
	}

    public class RootContainer : Container
    {
        public IConsole Console { get; private set; }

		public Component FocusedComponent { get; private set; }

        public InputController FocusedController { get; private set; }
        
		private Thread EventThread;

		private BlockingCollection<Tuple<InterfaceEvent, object, EventArgs>> Events =
			new BlockingCollection<Tuple<InterfaceEvent, object, EventArgs>>();

        public RootContainer(IConsole console)
        {
            ComponentAdded += CheckCount;
            ComponentRemoved += CheckCount;

			RequestRedraw += CreateHandler<RedrawEventArgs> (InterfaceEvent.REDRAW);

			RequestFocus += CreateHandler<ComponentEventArgs> (InterfaceEvent.SET_FOCUS);

			EventThread = new Thread (new ThreadStart (HandleEvents))
			{
				IsBackground = true,
				Name = "RootContainerEvents"
			};

			EventThread.Start();

            Console = console;
        }

		private void HandleEvents()
		{
			while (true)
			{
				Tuple<InterfaceEvent, object, EventArgs> evt = Events.Take ();

				System.Diagnostics.Debug.WriteLine ("Handling " + evt.Item1);

                switch (evt.Item1)
                {
                    case InterfaceEvent.REDRAW:
                        Redraw(evt.Item2, evt.Item3 as RedrawEventArgs);
                        break;
                    case InterfaceEvent.SET_FOCUS:
                        SetFocus(evt.Item2, evt.Item3 as ComponentEventArgs);
                        break;
                    case InterfaceEvent.SET_CONTROLLER:
                        SetController(evt.Item2, evt.Item3 as ControllerEventArgs);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
			}
		}

        private void CheckCount(object sender, ComponentEventArgs e)
        {
            if (Components.Count > 1)
            {
                System.Diagnostics.Debug.WriteLine("Warning: RootContainer has more than one child");
            }
        }

		private void SetFocus(object sender, ComponentEventArgs component)
		{
			if (FocusedComponent != null)
			{
				FocusedComponent.HasFocus = false;
			}

			FocusedComponent = component.Component as Component;

			if (FocusedComponent != null)
			{
				FocusedComponent.HasFocus = true;
			}
		}

        private void SetController(object sender, ControllerEventArgs controller)
        {
            if(FocusedController != null)
            {
                FocusedController.HasFocus = false;
            }

            FocusedController = controller.Controller;

            if(FocusedController != null)
            {
                FocusedController.HasFocus = true;
            }
        }

        public void OnRequestController(object sender, ControllerEventArgs args)
        {
            Events.Add(new Tuple<InterfaceEvent, object, EventArgs>(InterfaceEvent.SET_CONTROLLER, sender, args));
        }

        public void RegisterController(InputController controller)
        {
            controller.RequestFocus += CreateHandler<ControllerEventArgs>(InterfaceEvent.SET_CONTROLLER);
        }

		private void Redraw(object sender, RedrawEventArgs e)
		{
			// if there's an area to redraw, redraw it
			// otherwise redraw the entire buffer
			if (e != null && e.HasArea)
			{
				Draw (e.Area.Left, e.Area.Top, e.Area.Width, e.Area.Height);
			}
			else
			{
				Draw();
			}

            this.Console.SetCursorPosition(0, 0);
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

        #region implemented abstract members of IContainer

        protected override void DoLayoutImpl()
        {
            if (Components.Count > 0)
            {
                IComponent root = Components[0];

                root.Top = 0;
                root.Left = 0;
                root.Height = Console.BufferHeight;
                root.Width = Console.BufferWidth;

				if (root is Component)
				{
					(root as Component).Visible = true;
				}

            }

			for (int i = 1; i < Components.Count; i++)
			{
				if (Components [i] is Component)
				{
					(Components [i] as Component).Visible = false;
				}
			}
        }

        #endregion

        public override void Draw(IConsole buffer)
        {
            base.Draw(buffer);

            LineDrawer.GlobalDraw(buffer);
        }

        private EventHandler<T> CreateHandler<T>(InterfaceEvent evt) where T: EventArgs
		{
			return (object sender, T e) =>
				Events.Add (new Tuple<InterfaceEvent, object, EventArgs> (evt, sender, e));
		}

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

