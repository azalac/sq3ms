using System;
using System.Collections;
using System.Collections.Generic;

namespace SchedulingUI
{
	public interface IComponent
	{
		int ZIndex {get;}

		int Left { get; set; }
		int Top { get; set; }
		int Width { get; set; }
		int Height { get; set; }

		event EventHandler RequestRedraw;

		void Draw(IConsole buffer);
	}

	class Comparator<T, R> : IComparer<T>
		where R : IComparable
	{
		public Func<T, R> KeyExtractor { get; set; }

		public Comparator(Func<T, R> KeyExtractor)
		{
			this.KeyExtractor = KeyExtractor;
		}
		
		public int Compare (T x, T y)
		{
			return KeyExtractor (x).CompareTo (KeyExtractor (y));
		}
	}

	public abstract class IContainer : IComponent
	{
		public readonly List<IComponent> Components = new List<IComponent> ();

		private Comparator<IComponent, int> compare = new Comparator<IComponent, int> (c => c.ZIndex);

		public int Count {
			get { return Components.Count; }
		}

		public virtual void Add(IComponent component)
		{
			Components.Add (component);
			Components.Sort (compare);

			component.RequestRedraw += this.RequestRedraw;

			OnComponentAdded (this, new ComponentEventArgs (component));
		}

		public virtual void Add(params IComponent[] components)
		{
			
			Components.AddRange (components);
			Components.Sort (compare);

			foreach (IComponent c in components)
			{
				c.RequestRedraw += this.RequestRedraw;
				OnComponentAdded (this, new ComponentEventArgs (c));
			}

		}

		public void Remove(IComponent component)
		{
			component.RequestRedraw -= this.RequestRedraw;
			Components.Remove (component);

			OnComponentRemoved (this, new ComponentEventArgs (component));
		}

		public abstract void DoLayout ();

		public event EventHandler<ComponentEventArgs> OnComponentAdded;

		public event EventHandler<ComponentEventArgs> OnComponentRemoved;

		#region IComponent implementation
		
		public event EventHandler RequestRedraw;

		public void Draw (IConsole buffer)
		{
			// draws in order of the component's z-index
			foreach (IComponent component in Components)
			{
				component.Draw (buffer);
			}
		}

		public int ZIndex { get; set; }

		public int Left { get; set; }

		public int Top { get; set; }

		public int Width { get; set; }

		public int Height { get; set; }

		#endregion
	}

	public class ComponentEventArgs : EventArgs
	{
		public IComponent Component {get; private set;}

		public ComponentEventArgs(IComponent component)
		{
			Component = component;
		}
	}

	/// <summary>
	/// An interface which respresents a (software) write-only console.
	/// </summary>
	public interface IConsole
	{
		int BufferWidth {get;}
		int BufferHeight {get;}

		ConsoleColor Foreground {get; set;}
		ConsoleColor Background {get; set;}

		void SetCursorPosition (int x, int y);

		void PutCharacter(char c);
		void PutCharacter(int codepoint);

		void PutCharacter (int x, int y, char c);
		void PutCharacter(int x, int y, int codepoint);

	}

	public class StandardConsole : IConsole
	{
		public static readonly StandardConsole INSTANCE = new StandardConsole();

		private StandardConsole()
		{

		}

		#region IConsole implementation

		public void PutCharacter (char c)
		{
			Console.Write (c);
		}

		public void PutCharacter (int codepoint)
		{
			Console.Write (char.ConvertFromUtf32 (codepoint));
		}

		public void PutCharacter (int x, int y, char c)
		{
			SetCursorPosition(x, y);
			PutCharacter (c);
		}

		public void PutCharacter (int x, int y, int codepoint)
		{
			SetCursorPosition(x, y);
			PutCharacter (codepoint);
		}

		public void SetCursorPosition (int x, int y)
		{
			Console.SetCursorPosition (x, y);
		}

		public int BufferWidth {
			get {
				return Console.BufferWidth;
			}
		}

		public int BufferHeight {
			get {
				return Console.BufferHeight;
			}
		}

		public ConsoleColor Foreground {
			get {
				return Console.ForegroundColor;
			}
			set {
				Console.ForegroundColor = value;
			}
		}

		public ConsoleColor Background {
			get {
				return Console.BackgroundColor;
			}
			set {
				Console.BackgroundColor = value;
			}
		}

		#endregion


	}
}

