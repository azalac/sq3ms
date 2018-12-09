using Definitions;
using Support;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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
            
            Root.DoLayout();

            UserInput.StartThread();

            Root.Draw();

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

            AddContent(InitialContent);

            AddContent(new CancelRequestController());
            AddContent(new PersonDataEntry());



            WorkflowInitializer.SetupSchedulingContent(AddContent, WorkflowController);



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
    
    static class WorkflowInitializer
    {
        
        public static void SetupSchedulingContent(Action<object> content_adder, InterfaceWorkflowController controller)
        {
            SchedulingWorkflowInitializer.Initialize(content_adder, controller);

            BillingWorkflowInitializer.Initialize(content_adder, controller);
        }
        
    }

    static class SchedulingWorkflowInitializer
    {

        public static void Initialize(Action<object> content_adder, InterfaceWorkflowController controller)
        {
            
            ButtonSelector timeselector = new ButtonSelector("TimeslotSelect", 3, "Dummy");

            timeselector.BindButtonToOption("Dummy", "time", null);

            content_adder(timeselector);



            ButtonSelector searchtypeselect = new ButtonSelector("PersonSearchType", 2,
                "Search", "Add New Person");

            searchtypeselect.BindButtonToOption("Search", "option", "search");

            searchtypeselect.BindButtonToOption("Add New Person", "option", "addperson");

            content_adder(searchtypeselect);



            ButtonSelector hascaregiver = new ButtonSelector("AddCaregiver", 3,
                "Yes", "No");

            hascaregiver.BindButtonToOption("Yes", "hascaregiver", true);
            hascaregiver.BindButtonToOption("No", "hascaregiver", false);

            content_adder(hascaregiver);


            controller.AddWorkflow("Schedule Apt",
                "TimeslotSelect;" +
                "PersonSearchType(:Search for or Add Patient?);" +
                "PersonDataEntry(!searching);" +
                "AddCaregiver(:Add Caregiver to Appointment?)",
                AcceptScheduleData, ScheduleRedirect,
                null, PatientSearchTypeValidator, SearchPersonValidator, null);

            controller.AddWorkflow("Add Caregiver",
                "PersonSearchType(:Search for or Add Caregiver?);PersonDataEntry;",
                AcceptAddCaregiverData, null,
                CaregiverSearchTypeValidator, SearchPersonValidator);


        }

        private static Dictionary<string, object> AcceptScheduleData(Dictionary<string, object> data)
        {
            DebugLog.LogOther("Schedule Apt: " + string.Join(", ", from kvp in data select kvp.Key + "=" + kvp.Value));

            return null;
        }

        private static bool PatientSearchTypeValidator(Dictionary<string, object> values, out string message)
        {

            if (values.ContainsKey("option"))
            {
                values["searching"] = InterfaceWorkflowController.CheckEquals(values, "option", "search");
                values["patient_option"] = values["option"];
            }

            message = "";

            return true;
        }

        private static bool SearchPersonValidator(Dictionary<string, object> values, out string message)
        {
            message = "";

            // do nothing when adding a person
            if (InterfaceWorkflowController.CheckEquals(values, "searching", false))
            {
                return true;
            }

            // validate that person exists here

            return true;
        }

        private static bool CaregiverSearchTypeValidator(Dictionary<string, object> values, out string message)
        {
            if (values.ContainsKey("option"))
            {
                values["searching"] = InterfaceWorkflowController.CheckEquals(values, "option", "search");
                values["caregiver_option"] = values["option"];
            }

            message = "";

            return true;
        }

        private static string ScheduleRedirect(int stage, string stage_name, bool valid, Dictionary<string, object> values)
        {
            if (Equals(stage_name, "AddCaregiver") && valid && InterfaceWorkflowController.CheckEquals(values, "hascaregiver", true))
            {
                return "Add Caregiver";
            }

            return null;
        }

        private static Dictionary<string, object> AcceptAddCaregiverData(Dictionary<string, object> data)
        {
            DebugLog.LogOther("Add Caregiver: " + string.Join(", ", from kvp in data select kvp.Key + "=" + kvp.Value));

            return new Dictionary<string, object>() { };
        }

    }

    static class BillingWorkflowInitializer
    {

        public static void Initialize(Action<object> content_adder, InterfaceWorkflowController controller)
        {
            content_adder(new BillingCodeEntryController());

            ButtonSelector recall = new ButtonSelector("AppointmentRecall", 2, "None", "One Week", "Two Weeks", "Three Weeks");

            content_adder(recall);

            controller.AddWorkflow("Billing",
                "BillingCodeEntry;" +
                "AppointmentRecall;",
                AcceptData);

        }

        private static Dictionary<string, object> AcceptData(Dictionary<string, object> data)
        {
            DebugLog.LogOther("Billing: " + string.Join(", ", from kvp in data select kvp.Key + "=" + kvp.Value));

            return null;
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
            
            UpdateLabelText();
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
                UpdateLabelText();
                handled = true;
            }
            else if (args.Key.Key == ConsoleKey.K)
            {
                Week.Value++;
                UpdateLabelText();
                handled = true;
            }

            return handled;
            
        }

        private void UpdateLabelText()
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

        public void Activate(params string[] arguments)
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

    class BillingCodeEntryController : BinaryContainer, IInterfaceContent
    {
        private const ConsoleKey SCROLL_UP = ConsoleKey.U,
            SCROLL_DOWN = ConsoleKey.J;

        public string Name => "BillingCodeEntry";

        public event EventHandler<ReferenceArgs<Dictionary<string, object>>> Finish;

        private TextInput Input = new TextInput()
        {
            TextLength = 5
        };

        private Button AddButton = new Button()
        {
            Text = "Add"
        };

        private Button RemoveButton = new Button()
        {
            Text = "Remove"
        };

        private Button FinishButton = new Button()
        {
            Text = "Finish",
            Center = true,
            PreferredHeight = 2
        };

        private ScrollableContainer CodeDisplay = new ScrollableContainer()
        {
            ComponentHeight = 2,
            Background = ColorCategory.HIGHLIGHT_BG_2
        };

        private InputController controller = new InputController();
        
        private Dictionary<string, Label> Codes = new Dictionary<string, Label>();


        public BillingCodeEntryController()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            TernaryContainer controls = new TernaryContainer()
            {
                Vertical = true,
                PreferredWidth = 20
            };

            GridContainer upper_controls = new GridContainer()
            {
                CountY = 3,
                PreferredHeight = 6
            };
            
            upper_controls.Add(Input, AddButton, RemoveButton);

            Label padding = new Label("Scroll with U and J")
            {
                Center = true
            };

            controls.First = upper_controls;
            controls.Second = padding;
            controls.Third = FinishButton;

            controls.Add(upper_controls, padding, FinishButton);

            First = controls;
            Second = CodeDisplay;

            Add(controls, CodeDisplay);

            controller.Add(Input, AddButton, RemoveButton, FinishButton);
            controller.Parent = this;

            AddButton.Action += AddCode_Action;

            RemoveButton.Action += RemoveCode_Action;

            FinishButton.Action += FinishButton_Action;

        }

        private void FinishButton_Action(object sender, ComponentEventArgs e)
        {
            Finish?.Invoke(this, new ReferenceArgs<Dictionary<string, object>>(
                new Dictionary<string, object> { ["codes"] = new List<string>(Codes.Keys) }
            ));
        }

        private void AddCode_Action(object sender, ComponentEventArgs e)
        {
            if(Input.Text.Length == 4)
            {
                Label label = new Label(Input.Text);

                Codes[Input.Text] = label;

                CodeDisplay.Add(label);

                CodeDisplay.DoLayout();

                Input.Clear();

                OnRequestRedraw(this, new RedrawEventArgs(Input));
                OnRequestRedraw(this, new RedrawEventArgs(CodeDisplay));
            }
        }

        private void RemoveCode_Action(object sender, ComponentEventArgs e)
        {
            if (Input.Text.Length == 4 && Codes.ContainsKey(Input.Text))
            {
                CodeDisplay.Remove(Codes[Input.Text]);

                Codes.Remove(Input.Text);

                Input.Clear();

                CodeDisplay.DoLayout();

                OnRequestRedraw(this, new RedrawEventArgs(Input));
                OnRequestRedraw(this, new RedrawEventArgs(CodeDisplay));
            }
        }

        protected override bool HandleKeyPress(object keyboard, ConsoleKeyEventArgs args)
        {
            if(args.Key.Key == SCROLL_UP)
            {
                CodeDisplay.ScrollUp();
                return true;
            }
            if(args.Key.Key == SCROLL_DOWN)
            {
                CodeDisplay.ScrollDown();
                return true;
            }
            return false;
        }

        private void InitializeCodes(string[] codes)
        {
            foreach (var kvp in Codes)
            {
                CodeDisplay.Remove(kvp.Value);
            }

            Codes.Clear();

            foreach (string code in codes)
            {
                Label label = new Label(code);

                CodeDisplay.Add(label);

                Codes[code] = label;
            }
        }

        public void Activate(params string[] arguments)
        {
            if(arguments != null && arguments.Length > 0)
            {
                InitializeCodes(arguments[0].Split(';').Select(s => s.Trim()).ToArray());
            }

            controller.Activate();
        }
        
        public void Deactivate()
        {
            controller.Deactivate();
        }

        public void Initialize(RootContainer root)
        {

        }
    }

}
