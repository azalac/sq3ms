using Definitions;
using Support;
using System;

namespace SchedulingUI
{
    public class Reference<T>
    {
        public T Value;

        public Reference(T initial = default(T))
        {
            Value = initial;
        }
    }

    public class InterfaceStart
    {
        private RootContainer root;

        private KeyboardInput input;

        private WeekHeader header;

        private TabbedPane Content = new TabbedPane();

		private Menu menu;

		private Calendar calendar;
        
        private TimeSlotSelectionController TimeSlotSelector = new TimeSlotSelectionController();

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

            BinaryContainer binaryContainer1 = new BinaryContainer()
			{
				Vertical = true
			};

            BinaryContainer binaryContainer2 = new BinaryContainer()
			{
				Vertical = false,
				PreferredHeight = 20
			};

            binaryContainer2.Add(menu, calendar);

            binaryContainer2.First = menu;
            binaryContainer2.Second = calendar;

            Content.Add(binaryContainer2);
            Content.Add(TimeSlotSelector);

            binaryContainer1.Add (header, Content);

            binaryContainer1.First = header;
            binaryContainer1.Second = Content;

            root.Add(binaryContainer1);

            
            calendar.CurrentWeek = header.Week;

            menu.Init(root);

            TimeSlotSelector.Init(root);

            Content.SetSelectedIndex(1);


            root.DoLayout();
            root.Draw();

            System.Diagnostics.Debug.WriteLine(menu.Width);

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
    
	class WeekHeader: BinaryContainer
	{
        public Reference<int> Week { get; private set; }

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

        public WeekHeader()
        {
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
            if (!Visible)
            {
                return;
            }

            if (args.Key.Key == ConsoleKey.I)
            {
                Week.Value--;
            }
            else if (args.Key.Key == ConsoleKey.K)
            {
                Week.Value++;
            }

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

	class Calendar: GridContainer
	{
        public Reference<int> CurrentWeek { get; set; }

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

