using Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulingUI
{

    /// <summary>
    /// The menu on the left side of the default screen.
    /// </summary>
    class Menu : GridContainer
    {
        public AppointmentScheduler Scheduler { get; set; }

        InputController controller = new InputController();

        private Button[] buttons;

        private string[] button_names;

        /// <summary>
        /// Invoked when an option is selected.
        /// </summary>
        public event EventHandler<ReferenceArgs<string>> Action;

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

                buttons[i].Action += Menu_Action; ;

                controller.Add(buttons[i]);
                Add(buttons[i]);
            }

            DrawBorders = true;

        }

        private void Menu_Action(object sender, ComponentEventArgs e)
        {
            Action?.Invoke(sender, new ReferenceArgs<string>((e.Component as Button).Text));
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

    public class ButtonEvent
    {
        public event EventHandler<ComponentEventArgs> Action;

        public void OnAction(object sender, ComponentEventArgs args)
        {
            Action?.Invoke(sender, args);
        }
    }

    public class ButtonSelector : GridContainer, IInterfaceContent
    {
        public string Name { get; protected set; }

        public event EventHandler<ReferenceArgs<Dictionary<string, object>>> Finish;

        protected InputController controller = new InputController();


        /// <summary>
        /// The message to display.
        /// </summary>
        public string Message
        {
            get
            {
                return message.Text;
            }
            set
            {
                message.Text = value;
            }
        }

        private Label message = new Label();



        private readonly Dictionary<string, ButtonEvent> events = new Dictionary<string, ButtonEvent>();

        private Button[] buttons;

        public ButtonEvent this[string button]
        {
            get
            {
                return events[button];
            }
        }


        public ButtonSelector(string name, int padding, params string[] button_labels) :
            this(padding, button_labels)
        {
            Name = name;
        }

        public ButtonSelector(int padding, params string[] button_labels)
        {
            controller.Parent = this;

            buttons = new Button[button_labels.Length];

            foreach (string str in button_labels)
            {
                events[str] = new ButtonEvent();
            }

            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i] = new Button()
                {
                    Text = button_labels[i],
                    Center = true
                };

                buttons[i].Action += this[button_labels[i]].OnAction;

                controller.Add(buttons[i]);
            }

            CountY = padding * 2 + 1 + button_labels.Length;

            for (int i = 0; i < padding; i++)
            {
                Add(new Label());
            }

            Add(message);

            Add(buttons);

        }

        public void SetActionFinishes(string buttonaction, string option_name, object option_value)
        {
            this[buttonaction].Action += (sender, args) => OnFinish(this, new Dictionary<string, object>() { [option_name] = option_value });
        }

        protected void OnFinish(object sender, Dictionary<string, object> args)
        {
            Finish?.Invoke(sender, new ReferenceArgs<Dictionary<string, object>>(args));
        }

        public void Activate()
        {
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

    public class CancelRequestController : ButtonSelector, IInterfaceContent
    {
        public CancelRequestController() :
            base(2, "Retry", "Cancel")
        {
            Name = "CancelRequest";

            base["Retry"].Action += Continue_Option;
            base["Cancel"].Action += Cancel_Option;
        }

        private void Cancel_Option(object sender, ComponentEventArgs args)
        {
            OnFinish(this, new Dictionary<string, object> { ["continue"] = false });
        }

        private void Continue_Option(object sender, ComponentEventArgs args)
        {
            OnFinish(this, new Dictionary<string, object> { ["continue"] = true });
        }

    }

}
