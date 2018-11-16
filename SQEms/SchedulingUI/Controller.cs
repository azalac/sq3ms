using Definitions;
using Support;
using System;

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
    /// The main class for the interface controllers
    /// </summary>
    public class InterfaceStart
    {
        private RootContainer root;

        private KeyboardInput input;

        private WeekHeader header;

        private TabbedPane Content = new TabbedPane();

		private Menu menu;

		private Calendar calendar;
        
        private TimeSlotSelectionController TimeSlotSelector = new TimeSlotSelectionController();

        /// <summary>
        /// Change this to be default false to see the TimeSlotSelector prototpy.
        /// </summary>
        private Reference<bool> OnHomeScreen = new Reference<bool>(true);

        public InterfaceStart(IConsole buffer)
        {
            root = new RootContainer(buffer);

            input = new KeyboardInput(root)
            {
                ExitKey = ConsoleKey.Escape
            };

            header = new WeekHeader();

			menu = new Menu ();

			calendar = new Calendar();

            // Contains the week header and the content
            BinaryContainer binaryContainer1 = new BinaryContainer()
			{
				Vertical = true
			};

            // Initial Content - Contains the menu and the calendar
            BinaryContainer binaryContainer2 = new BinaryContainer()
			{
				Vertical = false,
				PreferredHeight = 20
			};

            // SET UP CONTAINERS

            binaryContainer2.Add(menu, calendar);

            binaryContainer2.First = menu;
            binaryContainer2.Second = calendar;

            Content.Add(binaryContainer2);
            Content.Add(TimeSlotSelector);

            binaryContainer1.Add (header, Content);

            binaryContainer1.First = header;
            binaryContainer1.Second = Content;

            root.Add(binaryContainer1);
            
            // FINISHED CONTAINER SET UP



            calendar.CurrentWeek = header.Week;

            // change this to false to see the real initial screen
            if(!OnHomeScreen.Value)
            {
                Content.SetSelectedIndex(1);
                TimeSlotSelector.Init(root);
            }
            else
            {
                Content.SetSelectedIndex(0);
                menu.Init(root);
            }

            root.DoLayout();

            root.Draw();
            
            input.StartThread();
            
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

            KeyPress += HeaderWeek_Keypress;

        }

        private void HeaderWeek_Keypress(object sender, ConsoleKeyEventArgs args)
        {
            if (!Visible || !AcceptInput.Value)
            {
                return;
            }

            bool needsRedraw = false;

            if (args.Key.Key == ConsoleKey.I)
            {
                Week.Value--;
                needsRedraw = true;
            }
            else if (args.Key.Key == ConsoleKey.K)
            {
                Week.Value++;
                needsRedraw = true;
            }

            if (needsRedraw)
            {
                nav_instructions.Text = string.Format("I /\\\nMONTH {0}\nK \\/", Week.Value);


                for (int i = 0; i < CalendarInfo.WEEK_LENGTH; i++)
                {
                    int? count = Scheduler?.AppointmentCount(Week.Value, i);

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

                    OnRequestRedraw(this, new RedrawEventArgs(day_labels[i]));
                }

                OnRequestRedraw(this, new RedrawEventArgs(nav_instructions));
            }
        }
    }

    /// <summary>
    /// The menu on the left side of the initial screen.
    /// </summary>
    class Menu : GridContainer
    {
        public AppointmentScheduler Scheduler { get; set; }

        InputController controller = new InputController();
        
        private Button Patients = new Button()
        {
            Text = "Patients"
        };

        private Button Schedule = new Button()
        {
            Text = "Schedule"
        };

        private Button Third = new Button()
        {
            Text = "Third"
        };

        public Menu()
		{
            controller.Parent = this;

            controller.Add(Patients);
            controller.Add(Schedule);
            controller.Add(Third);
            
            CountY = 6;
			
			PreferredWidth = 13;

			DrawBorders = true;

			OuterBorders &= ~LineDrawer.RIGHT;
            
            Add (Patients, Schedule, Third);
        }

        public void Init(RootContainer root)
		{
            root.RegisterController(controller);
            controller.SetSelectedIndex(0);

            root.OnRequestController(this, new ControllerEventArgs(controller));
        }
        
    }

    /// <summary>
    /// The calendar on the right side of the home screen.
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
                    Tuple<int, int> apt = Scheduler?.GetPatientIDs(new AptTimeSlot(CurrentWeek.Value, day, slot));

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

