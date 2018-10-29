using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace SchedulingUI
{
    public interface IComponent
    {
        int ZIndex { get; }

        int Left { get; set; }
        int Top { get; set; }
        int Width { get; set; }
        int Height { get; set; }

		int PreferredWidth{ get; }
		int PreferredHeight{ get; }

		bool Visible { get; }

		/// <summary>
		/// A Redraw event is a 'bottom-up' event. A sub component invokes it,
		/// and it is handled by a parent component.
		/// </summary>
		event EventHandler<RedrawEventArgs> RequestRedraw;

		/// <summary>
		/// A KeyPress event is a 'top-down' event. A parent invokes it, and
		/// it is handled by all the children.
		/// </summary>
		event EventHandler<ConsoleKeyEventArgs> KeyPress;

        ConsoleColor Background { get; set; }
        ConsoleColor Foreground { get; set; }

        void Draw(IConsole buffer);
    }

	public interface IFocusable
	{
		bool HasFocus { get; set; }
	}

    class Comparator<T, R> : IComparer<T>
        where R : IComparable
    {
        public Func<T, R> KeyExtractor { get; set; }

        public Comparator(Func<T, R> KeyExtractor)
        {
            this.KeyExtractor = KeyExtractor;
        }

        public int Compare(T x, T y)
        {
            return KeyExtractor(x).CompareTo(KeyExtractor(y));
        }
    }

    public abstract class Component : IComponent
    {
        public Component()
        {
        }

        public event EventHandler<ComponentEventArgs> ComponentAdded;

        public event EventHandler<ComponentEventArgs> ComponentRemoved;

        public ConsoleColor Background { get; set; }
        public ConsoleColor Foreground { get; set; }

        public event EventHandler<RedrawEventArgs> RequestRedraw;

		public event EventHandler<ConsoleKeyEventArgs> KeyPress;

		public virtual void OnRequestRedraw(object sender, RedrawEventArgs args)
		{
			if (RequestRedraw != null)
			{
				RequestRedraw (sender, args);
			}
        }

        public virtual void OnComponentAdded(object sender, ComponentEventArgs args)
        {
			if (ComponentAdded != null)
			{
				ComponentAdded (sender, args);
			}
        }

        public virtual void OnComponentRemoved(object sender, ComponentEventArgs args)
        {
			if (ComponentRemoved != null)
			{
				ComponentRemoved (sender, args);
			}
        }

		public virtual void OnKeyPressed(object keyboard, ConsoleKeyEventArgs args)
		{
			if (KeyPress != null)
			{
				KeyPress (keyboard, args);
			}
		}

        public int ZIndex { get; set; }

        public int Left { get; set; }

        public int Top { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

		public bool Visible { get; set; }

		public int PreferredWidth{ get; set; }
		public int PreferredHeight{ get; set; }

        public abstract void Draw(IConsole buffer);

    }

    public abstract class Container : Component
    {
        public readonly List<IComponent> Components = new List<IComponent>();

        private Comparator<IComponent, int> compare = new Comparator<IComponent, int>(c => c.ZIndex);

        public int Count
        {
            get { return Components.Count; }
        }

        public virtual void Add(IComponent component)
        {
            Components.Add(component);
            Components.Sort(compare);

            component.RequestRedraw += this.OnRequestRedraw;

			if(component is Component){
				(component as Component).Visible = true;
				KeyPress += (component as Component).OnKeyPressed;
			}

            OnComponentAdded(this, new ComponentEventArgs(component));
        }

        public virtual void Add(params IComponent[] components)
        {

            Components.AddRange(components);
            Components.Sort(compare);

            foreach (IComponent c in components)
            {
				c.RequestRedraw += this.OnRequestRedraw;
				
				if(c is Component){
					(c as Component).Visible = true;
					KeyPress += (c as Component).OnKeyPressed;
				}

                OnComponentAdded(this, new ComponentEventArgs(c));
            }

        }

        public void Remove(IComponent component)
        {
            component.RequestRedraw -= this.OnRequestRedraw;
			
			if(component is Component){
				KeyPress += (component as Component).OnKeyPressed;
			}

            Components.Remove(component);

            OnComponentRemoved(this, new ComponentEventArgs(component));
        }

        public abstract void DoLayout();

        #region Component implementation

        public override void Draw(IConsole buffer)
        {

			buffer.Background = Background;
			buffer.Foreground = Foreground;

			// clear the background of this container
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    buffer.PutCharacter(x, y, ' ');
                }
            }

            // draws in order of the component's z-index
            foreach (IComponent component in Components)
            {
				if (component.Visible)
				{
					// save the console's colors
					ConsoleColor pre_fg = buffer.Foreground;
					ConsoleColor pre_bg = buffer.Background;


					component.Draw (buffer);


					// load the console's colors
					buffer.Foreground = pre_fg;
					buffer.Background = pre_bg;
				}
            }
        }

        #endregion
    }

    public class ComponentEventArgs : EventArgs
    {
        public IComponent Component { get; private set; }

        public ComponentEventArgs(IComponent component)
        {
            Component = component;
        }
    }

	public class RedrawEventArgs : EventArgs
	{
		public Rectangle Area { get; private set; }

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
    /// An interface which respresents a (application-side) write-only console.
    /// </summary>
    public interface IConsole
    {
        int BufferWidth { get; }
        int BufferHeight { get; }

        bool SupportsComplex { get; }

        ConsoleColor Foreground { get; set; }
        ConsoleColor Background { get; set; }

        void SetCursorPosition(int x, int y);

        void PutCharacter(char c);
        void PutCharacter(int codepoint);

        void PutCharacter(int x, int y, char c);
        void PutCharacter(int x, int y, int codepoint);
		
		void PutString (int x, int y, string s);
		void PutString (int x, int y, string s, int length);

		IConsole CreateSubconsole (int Left, int Top, int Width, int Height);

    }

    public class StandardConsole : IConsole
    {
        public static readonly StandardConsole INSTANCE = new StandardConsole();

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

        public ConsoleColor Foreground
        {
            get
            {
                return Console.ForegroundColor;
            }
            set
            {
                Console.ForegroundColor = value;
            }
        }

        public ConsoleColor Background
        {
            get
            {
                return Console.BackgroundColor;
            }
            set
            {
                Console.BackgroundColor = value;
            }
        }

        #endregion

    }

	public class Subconsole : IConsole
	{
		private IConsole parent;
		private int Left, Top, Width, Height;

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
		}

		public void PutCharacter (char c)
		{
			parent.PutCharacter (c);
		}

		public void PutCharacter (int codepoint)
		{
			parent.PutCharacter (codepoint);
		}

		public void PutCharacter (int x, int y, char c)
		{
			if (ValidPosition (x, y))
			{
				parent.PutCharacter (x, y, c);
			}
		}

		public void PutCharacter (int x, int y, int codepoint)
		{
			if (ValidPosition (x, y))
			{
				parent.PutCharacter (x, y, codepoint);
			}
		}
		
		public void PutString (int x, int y, string s)
		{
			if (ValidPosition (x, y))
			{
				parent.PutString (x, y, s);
			}
		}
		
		public void PutString (int x, int y, string s, int length)
		{
			if (ValidPosition (x, y))
			{
				parent.PutString (x, y, s, length);
			}
		}

		public IConsole CreateSubconsole (int Left, int Top, int Width, int Height)
		{
			return new Subconsole(this, Left, Top, Width, Height);
		}

		private bool ValidPosition(int x, int y)
		{
			return x >= Left && x < Left + Width && y >= Top && y < Top + Height;
		}

		public int BufferWidth {
			get {
				return parent.BufferWidth;
			}
		}

		public int BufferHeight {
			get {
				return parent.BufferHeight;
			}
		}

		public bool SupportsComplex {
			get {
				return parent.SupportsComplex;
			}
		}

		public ConsoleColor Foreground {
			get {
				return parent.Foreground;
			}
			set {
				parent.Foreground = value;
			}
		}

		public ConsoleColor Background {
			get {
				return parent.Background;
			}
			set {
				parent.Background = value;
			}
		}

		#endregion


	}
}

