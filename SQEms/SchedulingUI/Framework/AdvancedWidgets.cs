
using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Threading;

namespace SchedulingUI
{
    /// <summary>
    /// A component which can be typed into.
    /// </summary>
	public class TextInput : Label
    {
        public enum TextLengthType
        {
            /// <summary>
            /// Default, text length relies only on its property.
            /// </summary>
            TEXT_LENGTH,

            /// <summary>
            /// Text length is the component's width only.
            /// </summary>
            WIDTH_ONLY,

            /// <summary>
            /// The component width, using TextLength as a right-side-padding.
            /// If <code>Width < TextLength</code>, the length is 0.
            /// </summary>
            WIDTH_MINUS_LENGTH
        }

        /// <summary>
        /// The currently-selected index.
        /// </summary>
		private int selectedIndex;

        /// <summary>
        /// The maximum length for text. Allows for 'TextLength - 1' characters
        /// in the Text property.
        /// </summary>
		public int TextLength { get; set; }

        /// <summary>
        /// Whether this text input has special highligh colors or not
        /// </summary>
        public bool SpecialHighlight { get; set; }

        /// <summary>
        /// The special highlight foreground.
        /// </summary>
        public ColorCategory HighlightForeground { get; set; }

        /// <summary>
        /// The special highligh background.
        /// </summary>
        public ColorCategory HighlightBackground { get; set; }

        /// <summary>
        /// The cursor color.
        /// </summary>
        /// <remarks>
        /// Always the same, regardless of highlighting/focis.
        /// </remarks>
        public ColorCategory CursorColor { get; set; }

        /// <summary>
        /// The way text length should be resolved.
        /// </summary>
        public TextLengthType TextLengthBinding { get; set; }

        public TextInput(int TextLength = 10)
        {
            Text = "";
            this.TextLength = TextLength;
        }

        /// <summary>
        /// Clears this text input.
        /// </summary>
        public void Clear()
        {
            Text = "";
            selectedIndex = 0;
        }

        protected override bool HandleKeyPress(object sender, ConsoleKeyEventArgs args)
        {
            // if this component doesn't have focus, don't do the keypress
            if (!HasFocus)
            {
                return false;
            }

            bool handled = false;

            string text_pre = Text;

            List<Tuple<int, int>> modified = new List<Tuple<int, int>>();

            modified.Add(new Tuple<int, int>(Left + selectedIndex, Top));

            switch (args.Key.Key)
            {
                case ConsoleKey.Backspace:
                    handled = Delete();
                    break;

                case ConsoleKey.LeftArrow:
                    if (selectedIndex > 0)
                    {
                        selectedIndex--;
                        handled = true;
                    }
                    break;

                case ConsoleKey.RightArrow:
                    if (selectedIndex < Text.Length)
                    {
                        selectedIndex++;
                        handled = true;
                    }
                    break;
            }

            if (!char.IsControl(args.Key.KeyChar))
            {
                handled = Insert(args.Key.KeyChar);
            }

            modified.Add(new Tuple<int, int>(Left + selectedIndex, Top));

            OnRequestRedraw(this, new RedrawEventArgs(Rectangle.Encompassing(modified)));

            // if the text has changed, it needs to be redrawn.
            if (!Text.Equals(text_pre))
            {
                handled = true;
            }

            return handled;
        }

        private int GetTargetTextLength()
        {
            switch (TextLengthBinding)
            {
                case TextLengthType.TEXT_LENGTH:
                    return TextLength;
                case TextLengthType.WIDTH_ONLY:
                    return Width;
                case TextLengthType.WIDTH_MINUS_LENGTH:
                    return Math.Max(0, Width - TextLength);
            }

            return TextLength;
        }

        private bool Delete()
        {
            // if there's a character to delete, delete it
            if (Text.Length > 0 && selectedIndex > 0)
            {
                selectedIndex--;

                Text = Text.Remove(selectedIndex, 1);

                return true;
            }

            return false;
        }

        private bool Insert(char c)
        {
            // if there's room to insert the character, insert it
            if (Text.Length + 1 < GetTargetTextLength())
            {
                Text = Text.Insert(selectedIndex, c.ToString());

                selectedIndex++;

                return true;
            }

            return false;
        }

        /// <summary>
        /// Draws this text input
        /// </summary>
        public override void Draw(IConsole buffer)
        {
            int target_length = GetTargetTextLength();

            int xoffset = Center ? (Width - target_length) / 2 : 0;

            string drawtext = Text + new string(' ', target_length - Text.Length);

            string pre = selectedIndex > 0 ? drawtext.Substring(0, selectedIndex) : "";
            char sel = drawtext[selectedIndex];
            string post = selectedIndex < drawtext.Length - 1 ? drawtext.Substring(selectedIndex + 1) : "";

            UpdateColors(buffer, 0);
            buffer.PutString(Left + xoffset, Top, pre);

            UpdateColors(buffer, selectedIndex);
            buffer.PutCharacter(Left + selectedIndex + xoffset, Top, sel);

            UpdateColors(buffer, selectedIndex + 1);
            buffer.PutString(Left + selectedIndex + 1 + xoffset, Top, post);
        }

        /// <summary>
        /// Updates the colors of the buffer depending on the current index of
        /// the text.
        /// </summary>
        /// <param name="buffer">The buffer to update</param>
        /// <param name="current_index">The index in the string</param>
        private void UpdateColors(IConsole buffer, int current_index)
        {
            if (HasFocus)
            {
                buffer.Foreground = SpecialHighlight ? HighlightForeground : Background;
                buffer.Background = SpecialHighlight ? HighlightBackground : Foreground;
            }
            else
            {
                buffer.Foreground = Foreground;
                buffer.Background = Background;
            }

            if (current_index == selectedIndex)
            {
                buffer.Background = ColorCategory.HIGHLIGHT_BG;
            }
        }
    }

    /// <summary>
    /// A component which can be activated with the 'Enter' key.
    /// </summary>
	public class Button : Label
    {
        /// <summary>
        /// The milliseconds since a button was last activated. Used to
        /// de-bounce buttons, in order to prevent multiple from being pressed
        /// in order when the user didn't want to.
        /// </summary>
        private static double LAST_ACTIVATION = 0;

        /// <summary>
        /// The milliseconds between activations.
        /// </summary>
        private static long ACTIVATION_DELAY = 20;

        /// <summary>
        /// This probably doesn't need its own static variable, but it helps
        /// with code description.
        /// </summary>
        private static DateTime UNIX_EPOCH = new DateTime(1970, 1, 1);
        
        /// <summary>
        /// An event which is invoked if this component is focused, and is visible.
        /// Only 'Enter' can invoke this event.
        /// </summary>
        public event EventHandler<ComponentEventArgs> Action;

        public Button()
        {
			Text = "Button";
        }

        protected override bool HandleKeyPress(object sender, ConsoleKeyEventArgs args)
		{
            bool handled = false;

			if (HasFocus && args.Key.Key == ConsoleKey.Enter)
            {
                double current_millis = (DateTime.UtcNow - UNIX_EPOCH).TotalMilliseconds;

                if (current_millis - LAST_ACTIVATION > ACTIVATION_DELAY)
                {
					if (Action != null)
					{
						Action (sender, new ComponentEventArgs (this));
					}
					else
					{
                        DebugLog.LogComponent("Warning: button with text '" + Text + "' has no action");
					}

                    LAST_ACTIVATION = current_millis;

                    handled = true;
                }
			}

            return handled;
		}

		public override void UpdateColors (IConsole buffer)
		{
			buffer.Foreground = HasFocus ? Background : Foreground;
			buffer.Background = HasFocus ? Foreground : Background;
		}

	}
    
    /// <summary>
    /// Controls multiple input controls, allowing the user to select between them.
    /// </summary>
    /// <remarks>
    /// This is not a component. It controls the components, but has no visual aspect.
    /// Components that should be controlled by it need to be added to its internal list.
    /// 
    /// Other InputControllers can be nested within InputControllers, but they
    /// should only be nested one level to prevent odd input handling.
    /// </remarks>
    public class InputController : IEnumerable
    {
        /// <summary>
        /// The component this controller uses to determine if it should accept
        /// input or not (based on visibility). Also hooks into the parent's
        /// keypress event.
        /// </summary>
        public Component Parent {
            get
            {
                return parent;
            }
            set
            {
                if(parent != null)
                {
                    parent.PriorityKeyPress -= HandleKeypress;
                }

                parent = value;
                parent.PriorityKeyPress += HandleKeypress;
            }
        }
        
        /// <summary>
        /// If this input should use Up+Down arrows (false) or Left+Right arrows (true).
        /// </summary>
        public bool Horizontal { get; set; }
        
        /// <summary>
        /// Invoked whenever the selection changes.
        /// </summary>
        public event EventHandler<ReferenceArgs<int>> SelectionChange;

        private readonly List<object> Components = new List<object>();
        
        private Component parent;

        private int selectedIndex = 0;

        private bool active = false;

        /// <summary>
        /// The keys which control all inputs. Numpad arrows are used to
        /// navigate (arrows conflict with TextInput).
        /// </summary>
        public const ConsoleKey VERT_INC = ConsoleKey.NumPad2,
                                VERT_DEC = ConsoleKey.NumPad8,
                                HORZ_INC = ConsoleKey.NumPad6,
                                HORZ_DEC = ConsoleKey.NumPad4;

        private void HandleKeypress(object sender, ConsoleKeyEventArgs args)
        {
            if (!Parent.Visible || !active)
            {
                return;
            }
            
            ConsoleKey Dec = Horizontal ? HORZ_DEC : VERT_DEC;
            ConsoleKey Inc = Horizontal ? HORZ_INC : VERT_INC;

            if (args.Key.Key == Dec)
            {
                SetSelectedIndex(Mod(selectedIndex - 1, Components.Count));
                args.Handled = true;
            }
            else if (args.Key.Key == Inc)
            {
                SetSelectedIndex(Mod(selectedIndex + 1, Components.Count));
                args.Handled = true;
            }
        }

        /// <summary>
        /// Sets the currently-selected component or controller.
        /// </summary>
        /// <param name="newSelection"></param>
        public void SetSelectedIndex(int newSelection)
        {
            // do nothing if there are no components
            if(Components.Count == 0 || newSelection == -1)
            {
                selectedIndex = -1;
                return;
            }

            DebugLog.LogComponent("New Selection: " + newSelection);

            int oldSelection = selectedIndex;
            selectedIndex = newSelection;

            // Update selection focus
            SetActive(oldSelection, false);
            SetActive(selectedIndex, true);

            // Try to invoke SelectionChange
            if (SelectionChange != null)
            {
                SelectionChange(this, new ReferenceArgs<int>(selectedIndex));
            }

        }

        public int GetSelectedIndex()
        {
            return selectedIndex;
        }

        /// <summary>
        /// Adds a component to this controller
        /// </summary>
        /// <param name="component">The component</param>
        public void Add(IComponent component)
        {
            Components.Add(component);
        }

        /// <summary>
        /// Adds multiple components to this controller
        /// </summary>
        /// <param name="component">The components</param>
        public void Add(params IComponent[] component)
        {
            Components.AddRange(component);
        }

        /// <summary>
        /// Adds a controller to this controller (should only be one level of
        /// nesting).
        /// </summary>
        /// <param name="controller">The controller</param>
        public void Add(InputController controller)
        {
            Components.Add(controller);
        }
        
        /// <summary>
        /// Activates this controller, and focuses its selected component.
        /// </summary>
        public void Activate()
        {
            active = true;

            SetActive(selectedIndex, true);
        }

        /// <summary>
        /// Deactivates this controller, and unfocuses its selected component.
        /// </summary>
        public void Deactivate()
        {
            active = false;

            SetActive(selectedIndex, false);
        }

        private void SetActive(int component, bool active)
        {
            if (component >= 0 && component < Components.Count)
            {
                if (active)
                {
                    if (Components[component] is IComponent c)
                    {
                        // Redraw & focus it if it's a component
                        parent.OnRequestFocus(this, new ComponentEventArgs(c));
                        parent.OnRequestRedraw(this, new RedrawEventArgs(c));
                    }
                    else if (Components[component] is InputController cont)
                    {
                        // activate if it's a controller
                        cont.Activate();
                    }

                }
                else
                {
                    if (Components[component] is IComponent c)
                    {
                        // Redraw & unfocus it if it's a component
                        parent.OnRequestFocus(this, new ComponentEventArgs(null));
                        parent.OnRequestRedraw(this, new RedrawEventArgs(c));
                    }
                    else if (Components[component] is InputController cont)
                    {
                        // deactivate it if it's a controller
                        cont.Deactivate();
                    }
                }
            }
        }

        private static int Mod(int a, int n)
        {
            return (a % n + n) % n;
        }

        /// <summary>
        /// Gets an enumerator of all components.
        /// </summary>
        /// <returns>The enumerator</returns>
        public IEnumerator GetEnumerator()
        {
            return Components.GetEnumerator();
        }
    }

    /// <summary>
    /// A container which only shows one child at a time.
    /// </summary>
    public class TabbedPane : Container
	{
        /// <summary>
        /// The currently selected child.
        /// </summary>
		public int SelectedIndex { get; private set; }

		public void SetSelectedIndex(int index)
		{
			SelectedIndex = index;
			DoLayout();
			OnRequestRedraw(this, new RedrawEventArgs(this));
		}

        public void SetSelected(IComponent c)
        {
            SelectedIndex = Components.IndexOf(c);
            DoLayout();
            OnRequestRedraw(this, new RedrawEventArgs(this));
        }

        #region implemented abstract members of Container

        /// <summary>
        /// Only sets the selected component to visible.
        /// All children completely fill the container.
        /// </summary>
        protected override void DoLayoutImpl ()
		{
            for (int i = 0; i < Components.Count; i++)
            {
                IComponent icomp = Components[i];

                icomp.Top = Top;
                icomp.Left = Left;
                icomp.Width = Width;
                icomp.Height = Height;

				if (icomp is Component)
				{
					(icomp as Component).Visible = i == SelectedIndex;
				}

            }
		}

		#endregion


	}

    public class ScrollableContainer : Container
    {
        public int ComponentHeight { get; set; }

        private int TotalHeight
        {
            get
            {
                return ComponentHeight * Components.Count;
            }
        }

        private int scroll;

        public void ScrollUp()
        {
            scroll--;
            if(scroll < 0)
            {
                scroll = 0;
            }
        }

        public void ScrollDown()
        {
            scroll++;
            if(scroll + Height > TotalHeight)
            {
                scroll = TotalHeight - Height;
            }
        }

        public void ScrollTo(IComponent c)
        {
            int i = Components.IndexOf(c);

            if(i == -1)
            {
                throw new ArgumentException("c is not a component in this scrollcontainer");
            }

            scroll = i * ComponentHeight;

            if (scroll < 0)
            {
                scroll = 0;
            }

            if (scroll + Height > TotalHeight)
            {
                scroll = TotalHeight - Height;
            }
        }

        protected override void DoLayoutImpl()
        {
            for(int i = 0; i < Components.Count; i++)
            {
                IComponent c = Components[i];

                c.Left = Left;
                c.Width = Width;

                c.Top = ComponentHeight * i - scroll + Top;
                c.Height = ComponentHeight;
            }
        }

        /// <summary>
        /// Only draws the components in this buffer.
        /// </summary>
        public override void Draw(IConsole buffer)
        {
            base.Draw(buffer.CreateSubconsole(Left, Top, Width, Height));
        }
    }

    /// <summary>
    /// An event args which contains an InputController
    /// </summary>
    public class ControllerEventArgs : EventArgs
    {
        /// <summary>
        /// The controller.
        /// </summary>
        public InputController Controller { get; private set; }

        public ControllerEventArgs(InputController controller)
        {
            Controller = controller;
        }
    }

    /// <summary>
    /// An event args which contains an object.
    /// </summary>
    public class ObjectEventArgs : EventArgs
    {
        /// <summary>
        /// The object.
        /// </summary>
        public object Value { get; private set; }

        public ObjectEventArgs(object Value = null)
        {
            this.Value = Value;
        }
    }

}
