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
    public class TimeSlotSelectionController : GridContainer
    {
        private readonly InputController controller = new InputController();

        private TextInput TimeSlot = new TextInput()
        {
            TextLength = 10,
            Center = true
        };

        private Button Today = new Button()
        {
            Text = "Today",
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
                                ControlSelector = new InputController() { Horizontal = true };

        private AptTimeSlot aptTimeSlot = new AptTimeSlot(0, 0, 0);

        public TimeSlotSelectionController()
        {
            CountX = 3;
            CountY = 7;

            // Timeslot entry
            Add(Label("TimeSlot #"), TimeSlot, Empty());

            // Today header
            Centered(Label("[Date]"));
            Centered(Today);
            Centered(Label("-- OR --"));

            // In X time
            Add(ByDay, Empty(), ByWeek);
            Add(Plus1, Plus2, Plus3);

            // Controll footer
            Add(Reset, DateVisual, Submit);

            // Setup controllers
            SpanSelector.Add(ByDay);
            SpanSelector.Add(ByWeek);

            AmountSelector.Add(Plus1);
            AmountSelector.Add(Plus2);
            AmountSelector.Add(Plus3);

            ControlSelector.Add(Reset);
            ControlSelector.Add(Submit);

            SpanSelector.Parent = this;
            AmountSelector.Parent = this;
            ControlSelector.Parent = this;
            controller.Parent = this;

            controller.Add(TimeSlot);
            controller.Add(Today);
            controller.Add(SpanSelector);
            controller.Add(AmountSelector);
            controller.Add(ControlSelector);

        }

        public void Init(RootContainer root)
        {
            root.RegisterController(controller);

            controller.SetSelectedIndex(0);
            SpanSelector.SetSelectedIndex(0);
            AmountSelector.SetSelectedIndex(0);
            ControlSelector.SetSelectedIndex(1);

            root.OnRequestController(this, new ControllerEventArgs(controller));
            
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

    }
}
