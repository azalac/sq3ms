using System;
using System.Collections.Generic;

namespace SchedulingUI
{
    /// <summary>
    /// The top-level of any component.
    /// Contains the bare minimum required to be classified as a component.
    /// </summary>
    public interface IComponent
    {

        /// <summary>
        /// The X position of this component.
        /// </summary>
        int Left { get; set; }

        /// <summary>
        /// The Y position of this component.
        /// </summary>
        int Top { get; set; }

        /// <summary>
        /// The width of this component.
        /// </summary>
        int Width { get; set; }

        /// <summary>
        /// The height of this component.
        /// </summary>
        int Height { get; set; }

        /// <summary>
        /// Whether this component is visible or not.
        /// </summary>
		bool Visible { get; set; }

        /// <summary>
        /// The preferred width of this component (used in layouts sometimes).
        /// </summary>
		int PreferredWidth { get; set; }

        /// <summary>
        /// The preferred height of this component (used in layouts sometimes).
        /// </summary>
		int PreferredHeight { get; set; }

        /// <summary>
        /// An 'upward branching' event which requests a specific area to be
        /// redrawn.
        /// </summary>
        /// <remarks>
        /// An upward branching event is one which is invoked by a child, and
        /// handled by all parent containers (in order).
        /// </remarks>
        event EventHandler<RedrawEventArgs> RequestRedraw;

        /// <summary>
        /// A 'downward branching' event which is invoked whenever the user
        /// presses a key.
        /// </summary>
        /// <remarks>
        /// A downward branching event is one which is invoked by a parent, and
        /// broadcasted to all children (in order).
        /// </remarks>
        event EventHandler<ConsoleKeyEventArgs> KeyPress;

        /// <summary>
        /// The background color of this component.
        /// </summary>
        ColorCategory Background { get; set; }

        /// <summary>
        /// The foreground color of this component.
        /// </summary>
        ColorCategory Foreground { get; set; }

        /// <summary>
        /// A method which is implemented by subclasses in order to draw the
        /// visual representation of the component.
        /// </summary>
        /// <param name="buffer">The buffer to draw to</param>
        void Draw (IConsole buffer);
    }
    
    /// <summary>
    /// An implementation of IComponent for simplicity.
    /// </summary>
    public abstract class Component : IComponent
    {
        public Component()
        {
			Background = ColorCategory.BACKGROUND;
			Foreground = ColorCategory.FOREGROUND;

			HasFocus = false;
        }

        /// <summary>
        /// An 'upward branching' event which requests a specific component to
        /// be focused.
        /// </summary>
        /// <remarks>
        /// An upward branching event is one which is invoked by a child, and
        /// handled by all parent containers (in order).
        /// </remarks>
		public event EventHandler<ComponentEventArgs> RequestFocus;

        /// <summary>
        /// An 'upward branching' event which requests a specific area to be
        /// redrawn.
        /// </summary>
        /// <remarks>
        /// An upward branching event is one which is invoked by a child, and
        /// handled by all parent containers (in order).
        /// </remarks>
        public event EventHandler<RedrawEventArgs> RequestRedraw;

        /// <summary>
        /// A 'downward branching' event which is invoked whenever the user
        /// presses a key.
        /// </summary>
        /// <remarks>
        /// A downward branching event is one which is invoked by a parent, and
        /// broadcasted to all children (in order).
        /// </remarks>
        public event EventHandler<ConsoleKeyEventArgs> KeyPress;

        /// <summary>
        /// The background color of this component.
        /// </summary>
        public ColorCategory Background { get; set; }

        /// <summary>
        /// The foreground color of this component.
        /// </summary>
        public ColorCategory Foreground { get; set; }

        /// <summary>
        /// Invokes <see cref="RequestRedraw"/>.
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="args">The area to redraw</param>
		public virtual void OnRequestRedraw(object sender, RedrawEventArgs args)
		{
			if (RequestRedraw != null)
			{
				RequestRedraw (sender, args);
			}
        }

        /// <summary>
        /// Invokes <see cref="KeyPress"/>
        /// </summary>
        /// <param name="keyboard">The keyboard object which received the event</param>
        /// <param name="args">The key that was pressed</param>
		public virtual void OnKeyPressed(object keyboard, ConsoleKeyEventArgs args)
		{
			if (KeyPress != null)
			{
				KeyPress (keyboard, args);
			}
		}

        /// <summary>
        /// Invokes <see cref="RequestFocus"/>
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="new_focus">The component to be focused</param>
		public virtual void OnRequestFocus(object sender, ComponentEventArgs new_focus)
		{
			if (RequestFocus != null)
			{
				RequestFocus(sender, new_focus);
			}
		}

        /// <summary>
        /// Updates the given buffer's colors to this component's colors.
        /// </summary>
        /// <param name="buffer">The buffer to update</param>
		public virtual void UpdateColors(IConsole buffer)
		{
			buffer.Foreground = Foreground;
			buffer.Background = Background;
		}
        
        /// <summary>
        /// The X position of this component.
        /// </summary>
        public int Left { get; set; }

        /// <summary>
        /// The Y position of this component.
        /// </summary>
        public int Top { get; set; }

        /// <summary>
        /// The width of this component.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// The height of this component.
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Whether this component is visible or not.
        /// </summary>
		public bool Visible { get; set; }

        /// <summary>
        /// The preferred width of this component (used in layouts sometimes).
        /// </summary>
		public int PreferredWidth{ get; set; }

        /// <summary>
        /// The preferred height of this component (used in layouts sometimes).
        /// </summary>
		public int PreferredHeight{ get; set; }

        /// <summary>
        /// Whether this component has focus or not.
        /// </summary>
		public bool HasFocus { get; set; }

        /// <summary>
        /// A method which is implemented by subclasses in order to draw the
        /// visual representation of the component.
        /// </summary>
        /// <param name="buffer">The buffer to draw to</param>
        public abstract void Draw(IConsole buffer);

    }

    /// <summary>
    /// The base class for a component which contains other components.
    /// </summary>
    public abstract class Container : Component
    {
        /// <summary>
        /// The list of all components in this container.
        /// Only exposed to sub-classes.
        /// </summary>
        protected readonly List<IComponent> Components = new List<IComponent>();
        
        /// <summary>
        /// The number of components in this container.
        /// </summary>
        public int Count
        {
            get { return Components.Count; }
        }

        /// <summary>
        /// Called when a component is added.
        /// </summary>
        public event EventHandler<ComponentEventArgs> ComponentAdded;

        /// <summary>
        /// Called when a component is removed.
        /// </summary>
        public event EventHandler<ComponentEventArgs> ComponentRemoved;

        /// <summary>
        /// Whether this container should redraw the background.
        /// Only exposed to sub-classes.
        /// Default is true.
        /// </summary>
		protected bool RedrawBackground { get; set; }

		public Container()
		{
			RedrawBackground = true;
		}

        /// <summary>
        /// Adds a single component.
        /// </summary>
        /// <param name="component">The component to add</param>
        public virtual void Add(IComponent component)
        {
            Components.Add(component);

			SetupHandlers (component);
        }

        /// <summary>
        /// Adds multiple components with a varargs argument list.
        /// </summary>
        /// <param name="components">The components to add</param>
        public virtual void Add(params IComponent[] components)
        {

            Components.AddRange(components);

            foreach (IComponent c in components)
            {
				SetupHandlers (c);
            }

        }

        /// <summary>
        /// Sets up any handlers for a given component.
        /// </summary>
        /// <param name="c"></param>
		private void SetupHandlers(IComponent c)
		{
			c.RequestRedraw += this.OnRequestRedraw;

			if(c is Component){
				(c as Component).Visible = true;
				KeyPress += (c as Component).OnKeyPressed;
				(c as Component).RequestFocus += this.OnRequestFocus;
			}

			OnComponentAdded(this, new ComponentEventArgs(c));
		}

        /// <summary>
        /// Removes a component from this container
        /// </summary>
        /// <param name="component">The component to remove</param>
        public void Remove(IComponent component)
        {
            component.RequestRedraw -= this.OnRequestRedraw;
			
			if(component is Component){
				KeyPress += (component as Component).OnKeyPressed;
			}

            Components.Remove(component);

            OnComponentRemoved(this, new ComponentEventArgs(component));
        }

        /// <summary>
        /// Invokes <see cref="ComponentAdded"/>.
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="args">The added component</param>
        public virtual void OnComponentAdded(object sender, ComponentEventArgs args)
        {
            if (ComponentAdded != null)
            {
                ComponentAdded(sender, args);
            }
        }

        /// <summary>
        /// Invokes <see cref="ComponentRemoved"/>.
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="args">The removed component</param>
        public virtual void OnComponentRemoved(object sender, ComponentEventArgs args)
        {
            if (ComponentRemoved != null)
            {
                ComponentRemoved(sender, args);
            }
        }

        /// <summary>
        /// The method which implementations override in order to layout their
        /// components.
        /// </summary>
        protected abstract void DoLayoutImpl();

        /// <summary>
        /// Calls <see cref="DoLayoutImpl"/>, then tries to layout any children,
        /// if they are containers.
        /// </summary>
		public void DoLayout()
		{
			DoLayoutImpl ();
			
			foreach(IComponent c in Components)
			{
				if (c is Container)
				{
					(c as Container).DoLayout ();
				}
			}

		}

        #region Component implementation

        /// <summary>
        /// Draws all children components, and possibly clears the background.
        /// </summary>
        /// <param name="buffer">The buffer to draw to</param>
        public override void Draw(IConsole buffer)
        {
			if (RedrawBackground)
			{
				buffer.Background = Background;
				buffer.Foreground = Foreground;

                //string filler = new string(' ', Width);
                
                // clear the background of this container
                for (int y = Top; y < Top + Height; y++)
                {
                    // TODO fix this
                    if (true)
                    {
                        for (int x = Left; x < Left + Width; x++)
                        {
                            buffer.PutCharacter(x, y, ' ');
                        }
                    }
                    else
                    {
                        //buffer.PutString(Left, y, filler);
                    }
                }
				
			}
            
            // If a component is visible, this saves the console's colors and draws it
            foreach (IComponent component in Components)
            {
				if (component.Visible)
				{
                    buffer.PushColors();

					if(component is Component)
					{
						(component as Component).UpdateColors (buffer);
					}

					component.Draw (buffer);

                    buffer.PopColors();
				}
            }
        }

        #endregion
    }

    /// <summary>
    /// An event args which contains an <see cref="IComponent"/>
    /// </summary>
    public class ComponentEventArgs : EventArgs
    {
        public IComponent Component { get; private set; }

        public ComponentEventArgs(IComponent component)
        {
            Component = component;
        }
    }

    /// <summary>
    /// An event args which possibly contains a <see cref="Rectangle"/> to redraw.
    /// </summary>
	public class RedrawEventArgs : EventArgs
	{
        /// <summary>
        /// The rectangle to redraw.
        /// </summary>
		public Rectangle Area { get; private set; }

        /// <summary>
        /// If this event args has an area.
        /// </summary>
		public bool HasArea { get; private set; }

		public RedrawEventArgs(Rectangle r)
		{
			Area = r;
			HasArea = true;
		}

		public RedrawEventArgs(IComponent c):
			this(new Rectangle(c))
		{
		}

		public RedrawEventArgs()
		{
			HasArea = false;
		}
	}

    /// <summary>
    /// An interface which respresents a write-only console. Does not necessarily represent a real console.
    /// </summary>
    public interface IConsole
    {
        /// <summary>
        /// The maximum width
        /// </summary>
        int BufferWidth { get; }

        /// <summary>
        /// The maximum height
        /// </summary>
        int BufferHeight { get; }

        /// <summary>
        //  Whether this console can support multi-byte characters.
        /// </summary>
        bool SupportsComplex { get; }

        /// <summary>
        /// Sets the foreground of the console
        /// </summary>
        ColorCategory Foreground { set; }

        /// <summary>
        /// Sets the background of the console
        /// </summary>
        ColorCategory Background { set; }

        /// <summary>
        /// Sets the cursor position of the console
        /// </summary>
        /// <param name="x">The X position</param>
        /// <param name="y">The Y position</param>
        void SetCursorPosition(int x, int y);

        /// <summary>
        /// Puts a character into the buffer
        /// </summary>
        /// <param name="c"></param>
        void PutCharacter(char c);

        /// <summary>
        /// Puts a character (via UTF-32) into the buffer
        /// </summary>
        /// <param name="codepoint">The UTF-32 encoded character</param>
        void PutCharacter(int codepoint);

        /// <summary>
        /// Puts a character at a specific position.
        /// </summary>
        /// <param name="x">The X position</param>
        /// <param name="y">The Y Position</param>
        /// <param name="c">The character</param>
        void PutCharacter(int x, int y, char c);

        /// <summary>
        /// Puts a character (via UTF-32) at a specific position.
        /// </summary>
        /// <param name="x">The X position</param>
        /// <param name="y">The Y Position</param>
        /// <param name="c">The UTF-32 encoded character</param>
        void PutCharacter(int x, int y, int codepoint);
		
        /// <summary>
        /// Puts a string at a specific position.
        /// </summary>
        /// <param name="x">The X position</param>
        /// <param name="y">The Y position</param>
        /// <param name="s">The string</param>
		void PutString (int x, int y, string s);

        /// <summary>
        /// Puts a string, limited to a length, at a specific position.
        /// </summary>
        /// <param name="x">The X position</param>
        /// <param name="y">The Y position</param>
        /// <param name="s">The string</param>
        /// <param name="length">The max length</param>
        void PutString (int x, int y, string s, int length);

        /// <summary>
        /// Stores the current colors of this console in a stack.
        /// </summary>
        void PushColors();

        /// <summary>
        /// Restores the current colors of this console from a stack.
        /// </summary>
        void PopColors();

        /// <summary>
        /// Creates a console which only draws within the specified area.
        /// </summary>
        /// <param name="Left">The X position</param>
        /// <param name="Top">The Y position</param>
        /// <param name="Width">The width</param>
        /// <param name="Height">The height</param>
        /// <returns>The subconsole</returns>
		IConsole CreateSubconsole (int Left, int Top, int Width, int Height);

    }

    /// <summary>
    /// Represents the <see cref="Console"/> class.
    /// See the <see cref="IConsole"/> class for documentation.
    /// </summary>
    public class StandardConsole : IConsole
    {
        /// <summary>
        /// Only one instance of this class may exist.
        /// </summary>
        public static readonly StandardConsole INSTANCE = new StandardConsole();

        private Stack<Tuple<ConsoleColor, ConsoleColor>> colors = new Stack<Tuple<ConsoleColor, ConsoleColor>>();
        
        private ConsoleColor bg_last,
                             fg_last;

        private StandardConsole()
        {

        }

        #region IConsole implementation
        
        public void PutCharacter(char c)
        {
            Console.Write(c);
        }

        public void PutCharacter(int codepoint)
        {
            Console.Write(char.ConvertFromUtf32(codepoint));
        }

        public void PutCharacter(int x, int y, char c)
        {
            SetCursorPosition(x, y);
            PutCharacter(c);
        }

        public void PutCharacter(int x, int y, int codepoint)
        {
            SetCursorPosition(x, y);
            PutCharacter(codepoint);
        }

		public void PutString (int x, int y, string s)
		{
			SetCursorPosition (x, y);
			Console.Write (s);
		}

		public void PutString (int x, int y, string s, int length)
		{
			SetCursorPosition (x, y);
			Console.Write (s.ToCharArray(), 0, length);
		}

        public void SetCursorPosition(int x, int y)
		{
			Console.SetCursorPosition (x, y);
		}
		
		public IConsole CreateSubconsole (int Left, int Top, int Width, int Height)
		{
			return new Subconsole(this, Left, Top, Width, Height);
		}

        public void PushColors()
        {
            colors.Push(new Tuple<ConsoleColor, ConsoleColor>(fg_last, bg_last));
        }

        public void PopColors()
        {
            Tuple<ConsoleColor, ConsoleColor> c = colors.Pop();

            if (c.Item1 != fg_last)
            {
                Console.ForegroundColor = c.Item1;
                fg_last = c.Item1;
            }

            if (c.Item2 != bg_last)
            {
                Console.BackgroundColor = c.Item2;
                bg_last = c.Item2;
            }
        }

        public bool SupportsComplex
        {
            get
            {
				return !Console.OutputEncoding.IsSingleByte;
            }
        }

        public int BufferWidth
        {
            get
            {
                return Console.WindowWidth;
            }
        }

        public int BufferHeight
        {
            get
            {
                return Console.WindowHeight;
            }
        }

        public ColorCategory Foreground
        {
            set
            {
                ConsoleColor requestedFg = ColorScheme.Current[value];

                if (requestedFg != fg_last)
                {
                    Console.ForegroundColor = requestedFg;
                    fg_last = requestedFg;
                }
            }
        }

        public ColorCategory Background
        {
            set
            {
                ConsoleColor requestedBg = ColorScheme.Current[value];

                if (requestedBg != bg_last)
                {
                    Console.BackgroundColor = requestedBg;
                    bg_last = requestedBg;
                }
            }
        }

        #endregion

    }

    /// <summary>
    /// A sub-console which only draws the characters within the specified area.
    /// See the <see cref="IConsole"/> class for documentation.
    /// </summary>
	public class Subconsole : IConsole
	{
		private IConsole parent;
		private int Left, Top, Width, Height;

        private int CursorX, CursorY;

		public Subconsole(IConsole parent, int Left, int Top, int Width, int Height)
		{
			this.parent = parent;
			this.Left = Left;
			this.Top = Top;
			this.Width = Width;
			this.Height = Height;
		}

		#region IConsole implementation

		public void SetCursorPosition (int x, int y)
		{
			parent.SetCursorPosition (x, y);
            UpdatePosition(x, y);
		}

		public void PutCharacter (char c)
		{
            if (ValidPosition(CursorX, CursorY))
            {
                parent.PutCharacter(c);
                NextPosition();
            }
            
		}

		public void PutCharacter (int codepoint)
		{
            if (ValidPosition(CursorX, CursorY))
            {
                parent.PutCharacter(codepoint);
                NextPosition();
            }
            
        }

        public void PutCharacter (int x, int y, char c)
		{
			if (ValidPosition (x, y))
			{
				parent.PutCharacter (x, y, c);
			}

            UpdatePosition(x, y);

        }

        public void PutCharacter (int x, int y, int codepoint)
		{
			if (ValidPosition (x, y))
			{
				parent.PutCharacter (x, y, codepoint);
			}

            UpdatePosition(x, y);

        }

        /// <summary>
        /// Puts a string with max length.
        /// <see cref="Subconsole.PutString(int, int, string, int)"/>
        /// </summary>
        /// <param name="x">The starting X</param>
        /// <param name="y">The starting Y</param>
        /// <param name="s">The string</param>
        public void PutString (int x, int y, string s)
		{
            PutString(x, y, s, s.Length);
        }

        /// <summary>
        /// Separates the string into chunks, which are then printed if they
        /// are inside the valid area.
        /// </summary>
        /// <param name="x">The starting X</param>
        /// <param name="y">The starting Y</param>
        /// <param name="s">The string</param>
        /// <param name="length">The maximum length</param>
        public void PutString (int x, int y, string s, int length)
		{
            int width = parent.BufferWidth;
            int offset = x + y * width;
            int i = 0;
            int i2 = 0;

            if(length < s.Length)
            {
                length = s.Length;
            }

            while (i2 < length)
            {
                while (!ValidPosition((i + offset) % width, (i + offset) / width) && i < length)
                {
                    i++;
                }

                i2 = i;

                while (ValidPosition((i2 + offset) % width, (i2 + offset) / width) && i2 < length)
                {
                    i2++;
                }

                parent.PutString((i + offset) % width, (i + offset) / width, s.Substring(i), i2 - i);
            }

            UpdatePosition((i2 + offset) % width, (i2 + offset) / width);
        }
        
		public IConsole CreateSubconsole (int Left, int Top, int Width, int Height)
		{
			return new Subconsole(this, Left, Top, Width, Height);
		}
        
        /// <summary>
        /// Checks if a position is within the valid area.
        /// </summary>
        /// <param name="x">The x position</param>
        /// <param name="y">The y position</param>
		private bool ValidPosition(int x, int y)
		{
			return x >= Left && x < Left + Width && y >= Top && y < Top + Height;
		}

        /// <summary>
        /// Moves the cursor to the next position.
        /// </summary>
        private void NextPosition()
        {
            CursorX++;

            if (CursorX >= BufferWidth)
            {
                CursorX = 0;
                CursorY++;
            }
        }

        /// <summary>
        /// Sets the cursor position.
        /// </summary>
        /// <param name="x">The x position</param>
        /// <param name="y">The y position</param>
        private void UpdatePosition(int x, int y)
        {
            CursorX = x;
            CursorY = y;
        }

        public void PushColors()
        {
            parent.PushColors();
        }

        public void PopColors()
        {
            parent.PopColors();
        }

        public int BufferWidth
        {
			get
            {
				return parent.BufferWidth;
			}
		}

		public int BufferHeight
        {
			get
            {
				return parent.BufferHeight;
			}
		}

		public bool SupportsComplex
        {
			get
            {
				return parent.SupportsComplex;
			}
		}

		public ColorCategory Foreground
        {
			set
            {
				parent.Foreground = value;
			}
		}

		public ColorCategory Background
        {
			set
            {
				parent.Background = value;
			}
		}


		#endregion


	}


    
}

