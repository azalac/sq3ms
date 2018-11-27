using Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulingUI
{
    /// <summary>
    /// Handles the interface for scheduling a patient.
    /// </summary>
    /// <remarks>
    /// Workflow:
    /// 
    /// Select a timeslot
    /// 
    /// Find the patient & caregiver
    /// 
    /// </remarks>
    public class SchedulingController
    {
    }

    /// <summary>
    /// Gets a timeslot from the user.
    /// </summary>
    /// <remarks>
    /// Workflow:
    /// 
    /// Select a date (today, +[1, 2, 3] [Days, Weeks])
    /// 
    /// Select a timeslot (1 - 6 & validation)
    /// 
    /// Finished
    /// 
    /// Layout:
    /// [TimeSlot] | [Input] | Empty
    /// Empty | [Date] | Empty
    /// Empty | [TodayButton] | Empty
    /// Empty | [Or Label] | Empty
    /// [Day Button] | Empty | [Week Button]
    /// [+1 Button] | [+2 Button] | [+3 Button]
    /// [Reset Button] | [Date Label] | [Confirm Button]
    /// </remarks>
    public class TimeSlotSelectionController : GridContainer, IInterfaceContent
    {
        private readonly InputController controller = new InputController();

        private LineDrawer lines = LineDrawer.FromGlobal();

        private TextInput TimeSlot = new TextInput()
        {
            TextLength = 10,
            Center = true
        };

        private TextInput WeekInput = new TextInput()
        {
            TextLength = 10,
            Center = true
        };

        private TextInput DayInput = new TextInput()
        {
            TextLength = 10,
            Center = true
        };

        private Button ByDay = new Button()
        {
            Text = "In Days",
            Center = true
        };

        private Button ByWeek = new Button()
        {
            Text = "In Weeks",
            Center = true
        };

        private Button Plus1 = new Button()
        {
            Text = "+1",
            Center = true
        };

        private Button Plus2 = new Button()
        {
            Text = "+2",
            Center = true
        };

        private Button Plus3 = new Button()
        {
            Text = "+3",
            Center = true
        };

        private Button Reset = new Button()
        {
            Text = "Reset",
            Center = true
        };

        private Label DateVisual = Label("???");

        private Button Submit = new Button()
        {
            Text = "Submit",
            Center = true
        };

        private InputController SpanSelector = new InputController() { Horizontal = true },
                                AmountSelector = new InputController() { Horizontal = true },
                                ControlSelector = new InputController() { Horizontal = true },
                                DateSelector = new InputController() { Horizontal = true };

        private List<Tuple<IComponent, ColorCategory>> HighlightedComponents = new List<Tuple<IComponent, ColorCategory>>();

        private AptTimeSlot aptTimeSlot = new AptTimeSlot(0, 0, 0);

        public string Name => "TimeSlot-Selector";

        public TimeSlotSelectionController()
        {
            CountX = 3;
            CountY = 5;

            // Timeslot entry
            Add(Label("Week"), Label("Day"), Label("TimeSlot"));
            Add(WeekInput, DayInput, TimeSlot);
            
            // In X time
            Add(ByDay, Empty(), ByWeek);
            Add(Plus1, Plus2, Plus3);

            // Control footer
            Add(Reset, DateVisual, Submit);

            // Setup controllers
            SpanSelector.Add(ByDay);
            SpanSelector.Add(ByWeek);

            AmountSelector.Add(Plus1);
            AmountSelector.Add(Plus2);
            AmountSelector.Add(Plus3);

            ControlSelector.Add(Reset);
            ControlSelector.Add(Submit);

            DateSelector.Add(WeekInput);
            DateSelector.Add(DayInput);
            DateSelector.Add(TimeSlot);

            SpanSelector.Parent = this;
            AmountSelector.Parent = this;
            ControlSelector.Parent = this;
            DateSelector.Parent = this;
            controller.Parent = this;
            
            controller.Add(DateSelector);
            controller.Add(SpanSelector);
            controller.Add(AmountSelector);
            controller.Add(ControlSelector);

            controller.SetSelectedIndex(0);
            SpanSelector.SetSelectedIndex(0);
            AmountSelector.SetSelectedIndex(0);
            ControlSelector.SetSelectedIndex(1);
            DateSelector.SetSelectedIndex(0);

            controller.SelectionChange += UpdateGrid;
        }

        private void UpdateGrid(object sender, ObjectEventArgs e)
        {
            foreach (Tuple<IComponent, ColorCategory> t in HighlightedComponents)
            {
                t.Item1.Background = t.Item2;
            }

            HighlightedComponents.Clear();

            if (e.Value is IComponent)
            {
                AddHighlighted(e.Value);
            }
            else if(e.Value is InputController controller)
            {
                foreach(object c in controller)
                {
                    AddHighlighted(c);
                }
            }
        }

        private void AddHighlighted(object obj)
        {
            if (obj is IComponent component)
            {
                HighlightedComponents.Add(new Tuple<IComponent, ColorCategory>(component, component.Background));

                component.Background = ColorCategory.HIGHLIGHT_BG_2;
            }
        }
        
        /// <summary>
        /// Creates an empty component which is used for spacing.
        /// </summary>
        /// <returns>The empty component</returns>
        private static IComponent Empty()
        {
            return new Label();
        }

        private static Label Label(string text)
        {
            return new Label(text)
            {
                Center = true
            };
        }

        /// <summary>
        /// Adds a component, which is centered.
        /// </summary>
        /// <param name="component">The component</param>
        /// <remarks>
        /// Adds two empty components: one before and one after.
        /// </remarks>
        private void Centered(IComponent component)
        {
            Add(Empty(), component, Empty());
        }

        public override void Draw(IConsole buffer)
        {
            base.Draw(buffer);
        }

        public void Initialize(RootContainer root)
        {

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
}
