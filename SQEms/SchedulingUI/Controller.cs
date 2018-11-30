using Definitions;
using Support;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SchedulingUI
{
    /// <summary>
    /// A reference to another object.
    /// </summary>
    /// <typeparam name="T">The type to reference</typeparam>
    public class Reference<T>
    {
        public T Value;

        public Reference(T initial = default(T))
        {
            Value = initial;
        }
    }

    /// <summary>
    /// An event args which references an object
    /// </summary>
    /// <typeparam name="T">The type to reference</typeparam>
    public class ReferenceArgs<T> : EventArgs
    {
        public T Value;

        public ReferenceArgs(T initial = default(T))
        {
            Value = initial;
        }
    }

    /// <summary>
    /// Controls multiple <see cref="IInterfaceContent"/>s and handles their
    /// activation.
    /// </summary>
    public class InterfaceContentController
    {
        private Dictionary<string, IInterfaceContent> content = new Dictionary<string, IInterfaceContent>();

        private IInterfaceContent last = null;

        public string Default { get; set; }

        public event EventHandler<ReferenceArgs<IInterfaceContent>> ContentChanged;

        public void Add(IInterfaceContent c)
        {
            content[c.Name] = c;
        }

        public void Activate(string name)
        {
            if(!content.ContainsKey(name))
            {
                throw new ArgumentException("Interface Content '" + name + "' not registered");
            }

            if(last != null)
            {
                last.Deactivate();
            }

            last = content[name];

            last.Activate();

            if (ContentChanged != null)
            {
                ContentChanged(this, new ReferenceArgs<IInterfaceContent>(last));
            }
        }

        public void Deactivate()
        {
            if(last != null)
            {
                last.Deactivate();
                last = null;
            }

            if (Default != null)
            {
                Activate(Default);
            }
            else
            {
                if (ContentChanged != null)
                {
                    ContentChanged(this, new ReferenceArgs<IInterfaceContent>(null));
                }
            }

        }

    }

    /// <summary>
    /// The main class for the interface controllers
    /// </summary>
    public class InterfaceStart
    {
        private RootContainer root;

        private KeyboardInput input;

        private WeekHeader header;

        private TabbedPane Content = new TabbedPane();

        private InterfaceContentController ContentController = new InterfaceContentController();

        private DefaultContent default_content;

        private TimeSlotSelectionController TimeSlotContent;

        /// <summary>
        /// Change this to be default false to see the TimeSlotSelector prototpy.
        /// </summary>
        private Reference<bool> OnHomeScreen = new Reference<bool>(true);

        public InterfaceStart(IConsole buffer)
        {
            InitializeRoot(buffer);

            InitializeContent();

            ContentController.Activate(TimeSlotContent.Name);

            root.DoLayout();

            root.Draw();
            
            input.StartThread();
        }
        
        /// <summary>
        /// Initializes the root components.
        /// </summary>
        private void InitializeRoot(IConsole buffer)
        {
            root = new RootContainer(buffer);

            input = new KeyboardInput(root)
            {
                ExitKey = ConsoleKey.Escape
            };

            header = new WeekHeader(OnHomeScreen);
            BinaryContainer root_container = new BinaryContainer()
            {
                Vertical = true
            };

            root_container.Add(header, Content);

            root_container.First = header;
            root_container.Second = Content;

            root.Add(root_container);
        }

        /// <summary>
        /// Initializes the content components.
        /// </summary>
        private void InitializeContent()
        {
            default_content = new DefaultContent(header.Week);

            TimeSlotContent = new TimeSlotSelectionController();

            ContentController.Add(default_content);
            ContentController.Add(TimeSlotContent);

            ContentController.ContentChanged += (object sender, ReferenceArgs<IInterfaceContent> args) =>
            {
                if(args.Value is IComponent c)
                {
                    Content.SetSelected(c);
                }
            };
            
            ContentController.Default = default_content.Name;

            Content.Add(default_content);
            Content.Add(TimeSlotContent);
        }

        public void WaitUntilExit()
        {
            input.InternalThread.Join();
        }

        public static void InitConsole()
        {
            Console.CursorVisible = false;
        }

        public static void ResetConsole()
        {
            Console.Clear();
            Console.ResetColor();
            Console.CursorVisible = true;
        }

    }
    
    /// <summary>
    /// An interface which represents content for the UI.
    /// </summary>
    public interface IInterfaceContent
    {
        /// <summary>
        /// This content's name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Initializes this interface content.
        /// </summary>
        /// <param name="root">The root container</param>
        void Initialize(RootContainer root);

        /// <summary>
        /// Activates this interface content.
        /// </summary>
        void Activate();

        /// <summary>
        /// Deactivates this interface content.
        /// </summary>
        void Deactivate();
    }

    /// <summary>
    /// The week header at the top of the console.
    /// </summary>
	class WeekHeader: BinaryContainer
	{
        /// <summary>
        /// The current week selection.
        /// </summary>
        public Reference<int> Week { get; private set; }

        /// <summary>
        /// The Scheduler object (used to update appointment counts)
        /// </summary>
        public AppointmentScheduler Scheduler { get; set; }

        private Label nav_instructions = new Label()
        {
            Text = "I /\\\nMONTH 0\nK \\/",
			DoWrapping = true,
            Center = true
        };

        private GridContainer nav_border = new GridContainer()
        {
            DrawBorders = true,
            PreferredWidth = 13
        };

        private GridContainer day_container = new GridContainer()
        {
            CountX = 7,
            DrawBorders = true
        };

        private Label[] day_labels = new Label[7];
        
        private static readonly string[] day_names = new string[]{ "SUN", "MON", "TUE", "WED", "THUR", "FRI", "SAT" };

        /// <summary>
        /// Whether this header should accept input (to change the current week).
        /// </summary>
        private Reference<bool> AcceptInput;

        public WeekHeader(Reference<bool> OnHomeScreen)
        {
            AcceptInput = OnHomeScreen;

            for (int i = 0; i < day_names.Length; i++)
            {
                day_labels[i] = new Label()
                {
                    Text = day_names[i],
                    Center = true,
                    PreferredWidth = 8
                };
            }

            nav_border.Add(nav_instructions);
            day_container.Add(day_labels);

            Add(nav_border);
            Add(day_container);

            First = nav_border;
            Second = day_container;
			
			PreferredHeight = 4;

            Vertical = false;

            Week = new Reference<int>(0);
            
            update_label_text();
        }

        protected override bool HandleKeyPress(object sender, ConsoleKeyEventArgs args)
        {
            if (!Visible || !AcceptInput.Value)
            {
                return false;
            }

            bool handled = false;

            if (args.Key.Key == ConsoleKey.I)
            {
                Week.Value--;
                update_label_text();
                handled = true;
            }
            else if (args.Key.Key == ConsoleKey.K)
            {
                Week.Value++;
                update_label_text();
                handled = true;
            }

            return handled;
            
        }

        private void update_label_text()
        {
            nav_instructions.Text = string.Format("I /\\\nMONTH {0}\nK \\/", Week.Value);

            OnRequestRedraw(this, new RedrawEventArgs(nav_instructions));

            for (int i = 0; i < CalendarInfo.WEEK_LENGTH; i++)
            {
                int? count = Scheduler?.AppointmentCount(0, Week.Value, i);

                string old_text = day_labels[i].Text;

                if (count.HasValue)
                {
                    day_labels[i].Text = day_names[i] + string.Format("\n\n{0}/{1}",
                        count.Value, CalendarInfo.MAX_APPOINTMENTS[i]);
                }
                else
                {
                    day_labels[i].Text = day_names[i] + string.Format("\n\n{0}/{1}",
                        "?", CalendarInfo.MAX_APPOINTMENTS[i]);
                }

                // only redraw label if necessary
                if (old_text != day_labels[i].Text)
                {
                    OnRequestRedraw(this, new RedrawEventArgs(day_labels[i]));
                }
            }

        }
    }

    class DefaultContent : BinaryContainer, IInterfaceContent
    {
        public string Name => "Default";

        private Menu menu = new Menu("Schedule Apt", "Billing", "Summary")
        {
            CountY = 6,
            PreferredWidth = 13,
            OuterBorders = LineDrawer.ALL & ~LineDrawer.RIGHT
        };

        private Calendar calendar = new Calendar();

        public DefaultContent(Reference<int> CurrentWeek)
        {
            calendar.CurrentWeek = CurrentWeek;

            Vertical = false;

            PreferredHeight = 20;

            First = menu;

            Second = calendar;

            Add(menu, calendar);
        }

        public void Initialize(RootContainer root)
        {

        }

        public void Activate()
        {
            menu.Activate();
        }

        public void Deactivate()
        {
            menu.Deactivate();
        }

    }

    /// <summary>
    /// The menu on the left side of the default screen.
    /// </summary>
    class Menu : GridContainer
    {
        public AppointmentScheduler Scheduler { get; set; }

        InputController controller = new InputController();

        private Button[] buttons;

        private string[] button_names;

        public Menu(params string[] button_names)
        {
            this.button_names = button_names;
            buttons = new Button[button_names.Length];

            controller.Parent = this;
            
            for (int i = 0; i < button_names.Length; i++)
            {
                buttons[i] = new Button()
                {
                    Text = button_names[i]
                };

                controller.Add(buttons[i]);
                Add(buttons[i]);
            }

            DrawBorders = true;

        }
        
        public void RegisterAction(string button, EventHandler<ComponentEventArgs> evt)
        {
            buttons[Array.IndexOf(button_names, button)].Action += evt;
        }

        public void RemoveAction(string button, EventHandler<ComponentEventArgs> evt)
        {
            buttons[Array.IndexOf(button_names, button)].Action -= evt;
        }

        public void Activate()
        {
            controller.Activate();
        }

        public void Deactivate()
        {
            controller.Deactivate();
        }
    }

    /// <summary>
    /// The calendar on the right side of the default screen.
    /// </summary>
	class Calendar: GridContainer
	{
        /// <summary>
        /// A reference to the current week. Used to get patient information.
        /// </summary>
        public Reference<int> CurrentWeek { get; set; }

        /// <summary>
        /// The Scheduler. Used to query appointments.
        /// </summary>
        public AppointmentScheduler Scheduler { get; set; }

        private readonly Label[] Labels;

		public Calendar()
		{
			CountX = CalendarInfo.WEEK_LENGTH;
			CountY = CalendarInfo.MAX_APPOINTMENTS2;
			
			PreferredWidth = 8 * CountX;

			DrawBorders = true;

			Labels = new Label[CountX * CountY];

			for (int i = 0; i < CountX * CountY; i++)
			{
                Labels[i] = new Label ()
				{
                    Center = true,
					DoWrapping = true
				};
			}

			Add (Labels);
		}

        /// <summary>
        /// Updates the calendar's labels.
        /// </summary>
        protected override void DoLayoutImpl()
        {
            base.DoLayoutImpl();
            
            for(int day = 0; day < CalendarInfo.WEEK_LENGTH; day++)
            {
                for(int slot = 0; slot < CalendarInfo.MAX_APPOINTMENTS[day]; slot++)
                {
                    Tuple<int, int> apt = Scheduler?.GetPatientIDs(new AptTimeSlot(1, CurrentWeek.Value, day, slot));

                    int i = slot * CountX + day;

                    if (apt != null)
                    {
                        Labels[i].Text = string.Format("{0}\n{1}", apt.Item1, apt.Item2);
                    }
                    else
                    {
                        Labels[i].Text = "";
                    }
                }
            }
            
        }

    }
}

