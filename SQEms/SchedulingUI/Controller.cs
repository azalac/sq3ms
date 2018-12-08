using Definitions;
using Support;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SchedulingUI
{
    /// <summary>
    /// The main class for the interface controllers
    /// </summary>
    public class InterfaceStart
    {
        private RootContainer Root;

        private KeyboardInput UserInput;

        private WeekHeader Header;

        private TabbedPane Content = new TabbedPane();

        private InterfaceContentController ContentController = new InterfaceContentController();

        private InterfaceWorkflowController WorkflowController;
        
        private Reference<bool> OnHomeScreen = new Reference<bool>(false);

        public InterfaceStart(IConsole buffer)
        {
            InitializeRoot(buffer);

            InitializeContent();

            InitializeWorkflows();

            Root.DoLayout();

            Root.Draw();
            
            UserInput.StartThread();
        }

        private void HandleMenuSelect(object sender, ReferenceArgs<Dictionary<string, object>> e)
        {
            WorkflowController.InvokeWorkflow((string)e.Value["option"]);
        }

        /// <summary>
        /// Initializes the root components.
        /// </summary>
        private void InitializeRoot(IConsole buffer)
        {
            Root = new RootContainer(buffer);

            UserInput = new KeyboardInput(Root)
            {
                ExitKey = ConsoleKey.Escape
            };

            Header = new WeekHeader(OnHomeScreen);
            BinaryContainer root_container = new BinaryContainer()
            {
                Vertical = true
            };

            root_container.Add(Header, Content);

            root_container.First = Header;
            root_container.Second = Content;

            Root.Add(root_container);

            WorkflowController = new InterfaceWorkflowController(ContentController);
        }

        /// <summary>
        /// Initializes the content components.
        /// </summary>
        private void InitializeContent()
        {
            DefaultContent InitialContent = new DefaultContent(Header.Week);

            InitialContent.Finish += HandleMenuSelect;

            TimeSlotSelectionController TimeSlotContent = new TimeSlotSelectionController();

            CancelRequestController cancel = new CancelRequestController();

            ButtonSelector select = new ButtonSelector("select", 2, "one", "two", "three");

            select.SetActionFinishes("one", "option", 1);
            select.SetActionFinishes("two", "option", 2);
            select.SetActionFinishes("three", "option2", 3);

            select.Message = "This is a message";

            AddContent(InitialContent);
            AddContent(TimeSlotContent);
            AddContent(cancel);
            AddContent(select);

            ContentController.ContentChanged += (object sender, ReferenceArgs<IInterfaceContent> args) =>
            {
                if(args.Value is IComponent c)
                {
                    Content.SetSelected(c);
                }

                OnHomeScreen.Value = args.Value.Name == "Default";
                
            };
            
            ContentController.Default = InitialContent.Name;

            ContentController.Activate(ContentController.Default);

        }

        private void InitializeWorkflows()
        {
            WorkflowController.AddWorkflow("Schedule Apt", "select;select", Accept, Validate, Validate);
        }
        
        private bool Validate(Dictionary<string, object> values, out string error_msg)
        {
            if(InterfaceWorkflowController.CheckEquals(values, "option", 1))
            {
                error_msg = "Option cannot be 1";
                return false;
            }

            error_msg = "";
            return true;
        }

        private void Accept(Dictionary<string, object> values)
        {
            Debug.WriteLine(values);
        }
        
        private void AddContent(object obj)
        {
            if(obj is IInterfaceContent icontent && obj is IComponent component)
            {
                ContentController.Add(icontent);
                Content.Add(component);
            }
            else
            {
                throw new ArgumentException("Object is not an IInterfaceContent or IComponent");
            }
        }

        public void WaitUntilExit()
        {
            UserInput.InternalThread.Join();
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
                //Added the 1 for the month to be search, should be changed
                int? count = Scheduler?.AppointmentCount(1, Week.Value, i);

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

        private Menu menu = new Menu("Schedule Apt", "Billing", "Summary", "Add Patient")
        {
            CountY = 6,
            PreferredWidth = 13,
            OuterBorders = LineDrawer.ALL & ~LineDrawer.RIGHT
        };

        private Calendar calendar = new Calendar();

        public event EventHandler<ReferenceArgs<Dictionary<string, object>>> Finish;

        public DefaultContent(Reference<int> CurrentWeek)
        {
            calendar.CurrentWeek = CurrentWeek;

            Vertical = false;

            PreferredHeight = 20;

            First = menu;

            Second = calendar;

            Add(menu, calendar);

            menu.Action += Finish_Action;
        }

        private void Finish_Action(object sender, ReferenceArgs<string> e)
        {
            Dictionary<string, object> values = new Dictionary<string, object>()
            {
                ["option"] = e.Value
            };

            Finish(this, new ReferenceArgs<Dictionary<string, object>>(values));
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
