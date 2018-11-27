using System;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace SchedulingUI
{
    /// <summary>
    /// A component which shows text. Handles newlines properly.
    /// </summary>
	public class Label : Component
	{
        /// <summary>
        /// The text to draw.
        /// </summary>
		public string Text { get; set; }

        /// <summary>
        /// If the text should be centered.
        /// </summary>
		public bool Center { get; set; }

        /// <summary>
        /// If the text should go to a newline.
        /// </summary>
		public bool DoWrapping { get; set; }

        /// <summary>
        /// Creates a new label
        /// </summary>
        /// <param name="Text">Optional text parameter</param>
		public Label(string Text = "")
		{
            this.Text = Text;
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

        /// <summary>
        /// Splits this label's text into chunks, with length determined by 
        /// newlines or the maximum width of the component.
        /// </summary>
        /// <remarks>
        /// The x and y positions are relative to the top-left of the label.
        /// </remarks>
        /// <returns>An IEnumerable with the x&y positions and the chunk</returns>
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

        /// <summary>
        /// Searches this label's text for a newline, or the max width in order
        /// to get the x offset for a chunk. Only used when centering.
        /// </summary>
        /// <remarks>
        /// Assumes the current index is the start of a line.
        /// </remarks>
        /// <param name="i">The current index</param>
        /// <param name="max_length">The maximum length to search.</param>
        /// <returns>The x offset of the current line</returns>
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
			IEnumerable<Tuple<int, int, string>> chunks = GetChunks();
			
			if (chunks.Count() == 0)
			{
				return new Rectangle (Left, Top, 0, 0);
			}

			Tuple<int, int, string> TopLeft = chunks.First();
			Tuple<int, int, string> BottomRight = chunks.Last();

            // Gets the top left position and the bottom right position, accounting for the string length
			return Rectangle.BetweenCoords (TopLeft.Item1 + Left, TopLeft.Item1 + Top, BottomRight.Item1 + Left,
                                           BottomRight.Item1 + Top + BottomRight.Item3.Length);
		}

		#region implemented abstract members of Component

        /// <summary>
        /// Gets all chunks and draws them. Does nothing if the component has
        /// no width or text.
        /// </summary>
        /// <param name="buffer">The buffer to draw to</param>
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

    /// <summary>
    /// Lays out children in an evenly-spaced grid.
    /// </summary>
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

    /// <summary>
    /// Lays out children in a vertical or horizontal line, with size determined
    /// by their preferred height or width.
    /// </summary>
    public class FlowContainer : Container
	{
		/// <summary>
		/// If this container draws and accounts for the borders between cells.
		/// </summary>
		public bool DrawBorders { get; set; }

        /// <summary>
        /// If this container should lay out children vertically or horizontally.
        /// </summary>
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

    /// <summary>
    /// Lays out two children vertically or horizontally. Only respects the first's
    /// preferred size, while the second gets the remaining area.
    /// </summary>
    public class BinaryContainer : Container
    {
        /// <summary>
        /// If this container should lay out children vertically or horizontally.
        /// </summary>
        public bool Vertical { get; set; }

        /// <summary>
        /// The first component (must be a child of the container)
        /// </summary>
        public IComponent First { get; set; }

        /// <summary>
        /// The second component (must be a child of the container)
        /// </summary>
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

    /// <summary>
    /// An enum which represents the events a RootContainer can handle.
    /// </summary>
    enum InterfaceEvent
	{
		REDRAW,
		SET_FOCUS
	}

    /// <summary>
    /// The top level container. Has a reference to the console, for easy drawing.
    /// Manages all focus-related properties, and has an internal thread for its
    /// events.
    /// </summary>
    public class RootContainer : Container
    {
        /// <summary>
        /// The referenced console.
        /// </summary>
        public IConsole Console { get; private set; }

        /// <summary>
        /// The currently focused component.
        /// </summary>
		public Component FocusedComponent { get; private set; }

        /// <summary>
        /// The currently focused controller.
        /// </summary>
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

        /// <summary>
        /// Redraws entire container.
        /// </summary>
        public void Draw()
        {
            Draw(Console);
        }

        /// <summary>
        /// Redraws specific area
        /// </summary>
        /// <param name="Left">The X position</param>
        /// <param name="Top">The Y position</param>
        /// <param name="Width">The width</param>
        /// <param name="Height">The height</param>
		public void Draw(int Left, int Top, int Width, int Height)
		{
			Draw (Console.CreateSubconsole (Left, Top, Width, Height));
		}

        /// <summary>
        /// Redraws specific component
        /// </summary>
        /// <param name="component">The component to redraw</param>
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

        /// <summary>
        /// draws all components, and redraws the lines.
        /// </summary>
        /// <param name="buffer"></param>
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

    /// <summary>
    /// An event args which contains a console key.
    /// </summary>
	public class ConsoleKeyEventArgs : EventArgs
	{
        /// <summary>
        /// The key that was pressed.
        /// </summary>
		public ConsoleKeyInfo Key { get; private set; }

		public ConsoleKeyEventArgs(ConsoleKeyInfo key)
		{
			Key = key;
		}
	}

}

