using Demographics;
using Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SchedulingUI
{

    /// <summary>
    /// The menu on the left side of the default screen.
    /// </summary>
    class Menu : GridContainer
    {
        public AppointmentScheduler Scheduler { get; set; }

        private InputController controller = new InputController();

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

        private InputController controller = new InputController();
        
        
        protected InputController Controller { get => controller; set => controller = value; }

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
            Controller.Parent = this;

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

                Controller.Add(buttons[i]);
            }

            CountY = padding * 2 + 1 + button_labels.Length;

            for (int i = 0; i < padding; i++)
            {
                Add(new Label());
            }

            Add(message);

            Add(buttons);

        }

        private List<Tuple<string, object>> debug_bindings = new List<Tuple<string, object>>();

        public void BindButtonToOption(string buttonaction, string option_name, object option_value)
        {
            // an attempt to fix variable capturing
            ActionInvoker invoker = new ActionInvoker()
            {
                sender = this,
                option = option_name,
                value = option_value
            };

            invoker.Action += OnFinish;

            debug_bindings.Add(new Tuple<string, object>(option_name, option_value));

            this[buttonaction].Action += invoker.HandleFinish;
        }
        
        protected void OnFinish(object sender, ReferenceArgs<Dictionary<string, object>> args)
        {
            DebugLog.LogController(args.Value);
            
            Finish?.Invoke(sender, args);
        }

        public void Activate(params string[] arguments)
        {
            if(arguments != null && arguments.Length > 0)
            {
                message.Text = arguments[0];
            }

            Controller.Activate();
        }

        public void Deactivate()
        {
            Controller.Deactivate();
        }

        public void Initialize(RootContainer root)
        {

        }

        private class ActionInvoker
        {
            public object sender;
            public string option;
            public object value;
            public event EventHandler<ReferenceArgs<Dictionary<string, object>>> Action;
            
            public void HandleFinish(object sender, ComponentEventArgs args)
            {
                Dictionary<string, object> values = new Dictionary<string, object>()
                {
                    [option] = value
                };

                Action?.Invoke(this.sender, new ReferenceArgs<Dictionary<string, object>>(values));
            }
        }
    }

    public abstract class FormInputSelectorContent : BinaryContainer, IInterfaceContent
    {
        /// <summary>
        /// A delegate which parses the given text to an object.
        /// </summary>
        /// <param name="text">The text to parse.</param>
        /// <param name="valid">Whether the text is valid or not.</param>
        /// <returns>The parsed object.</returns>
        protected delegate object InputParser(string text, out bool valid);

        private readonly InputParser IDENTITY_PARSER = (string text, out bool valid) => { valid = true; return text; };

        public string Name { get; protected set; }

        /// <summary>
        /// The minimum number of inputs required. Setting to -1 means all
        /// inputs must be valid.
        /// </summary>
        public int MinRequired { get; set; } = -1;

        protected Dictionary<string, InputParser> Parsers = new Dictionary<string, InputParser>();

        private InputController controller = new InputController();

        private Label[] labels;
        private TextInput[] inputs;
        private object[] values;
        private bool[] valid_inputs;

        private int max_label_length = 0;

        private BinaryContainer content;

        private GridContainer label_grid, input_grid;

        private Button submit = new Button()
        {
            Text = "OK",
            Center = true
        };

        public event EventHandler<ReferenceArgs<Dictionary<string, object>>> Finish;

        public FormInputSelectorContent(params string[] labels)
        {
            this.labels = new Label[labels.Length];
            inputs = new TextInput[labels.Length];
            values = new object[labels.Length];
            valid_inputs = new bool[labels.Length];
            
            for (int i = 0; i < labels.Length; i++)
            {
                this.labels[i] = new Label(labels[i])
                {
                    Center = true
                };

                inputs[i] = new TextInput()
                {
                    TextLengthBinding = TextInput.TextLengthType.WIDTH_MINUS_LENGTH,
                    TextLength = 5,
                    HighlightForeground = ColorCategory.ERROR_FG,
                    HighlightBackground = ColorCategory.FOREGROUND
                };

                controller.Add(inputs[i]);

                max_label_length = Math.Max(max_label_length, labels[i].Length);
            }

            controller.Parent = this;

            controller.Add(submit);

            SetupContainers(max_label_length);

            submit.Action += SubmitCheck;

            controller.SelectionChange += ValidateInput;
        }

        /// <summary>
        /// Calls <see cref="Finish"/> if enough inputs are valid, when submitted.
        /// Only valid inputs are passed to <see cref="Finish"/>.
        /// </summary>
        private void SubmitCheck(object sender, ComponentEventArgs e)
        {
            int min = MinRequired == -1 ? labels.Length : MinRequired;

            if (valid_inputs.Count(b => b) >= min && Finish != null)
            {
                Dictionary<string, object> vals = new Dictionary<string, object>();

                for(int i = 0; i < labels.Length; i++)
                {
                    if (valid_inputs[i])
                    {
                        vals[labels[i].Text] = values[i];
                    }
                }

                submit.Text = "OK";
                OnRequestRedraw(this, new RedrawEventArgs(submit));

                Finish(this, new ReferenceArgs<Dictionary<string, object>>(vals));
            }
            else
            {
                submit.Text = string.Format("Invalid Inputs ({0}/{1})", valid_inputs.Count(b => b), min);
                OnRequestRedraw(this, new RedrawEventArgs(submit));
            }
        }

        /// <summary>
        /// Parses all inputs and saves their objects to <see cref="values"/>,
        /// and their validity to <see cref="valid_inputs"/>.
        /// </summary>
        private void ValidateInput(object sender, EventArgs args)
        {
            for (int i = 0; i < labels.Length; i++)
            {
                string label = labels[i].Text;

                // try to get the parser, default to the identity parser if it doesn't exist
                InputParser parser = Parsers.ContainsKey(label) ? Parsers[label] : IDENTITY_PARSER;

                values[i] = parser(inputs[i].Text, out valid_inputs[i]);

                bool hide_error = valid_inputs[i] || inputs[i].Text.Length == 0;

                inputs[i].Foreground = hide_error ? ColorCategory.FOREGROUND : ColorCategory.ERROR_FG;
                inputs[i].Background = ColorCategory.BACKGROUND;

                inputs[i].SpecialHighlight = !hide_error;
            }
        }

        private void SetupContainers(int label_width)
        {
            content = new BinaryContainer()
            {
                PreferredHeight = GetContentHeight(),
                Vertical = false
            };

            label_grid = new GridContainer()
            {
                PreferredWidth = label_width + 2,
                CountY = labels.Length + 1
            };

            input_grid = new GridContainer()
            {
                CountY = labels.Length + 1
            };

            content.Add(label_grid, input_grid);

            content.First = label_grid;
            content.Second = input_grid;


            label_grid.Add(labels);

            input_grid.Add(inputs);
            input_grid.Add(submit);


            Label bottom_padding = new Label();

            this.Add(content, bottom_padding);

            First = content;
            Second = bottom_padding;

            Vertical = true;
        }

        private void RevalidateLayout()
        {
            label_grid.PreferredWidth = max_label_length + 2;
        }

        /// <summary>
        /// Gets the value for the given label.
        /// </summary>
        /// <param name="label">The label's text.</param>
        /// <returns>The TextInput's text</returns>
        public string this[string label]
        {
            get
            {
                for (int i = 0; i < labels.Length; i++)
                {
                    if (labels[i].Text.Equals(label))
                    {
                        return inputs[i].Text;
                    }
                }

                throw new ArgumentException("Label " + label + " was not found");
            }

            set
            {
                for (int i = 0; i < labels.Length; i++)
                {
                    if (labels[i].Text.Equals(label))
                    {
                        inputs[i].Text = value;
                    }
                }

                throw new ArgumentException("Label " + label + " was not found");
            }
        }

        public void SetLabelText(int i, string Text)
        {
            if (i < 0 || i >= labels.Length)
            {
                throw new IndexOutOfRangeException();
            }

            if (Text == null || Text.Length == 0)
            {
                throw new ArgumentException("Text is invalid");
            }

            int max = 0;

            foreach (Label l in labels)
            {
                if (Equals(Text, l.Text))
                {
                    throw new ArgumentException("The text is already in another label");
                }

                max = Math.Max(max, l.Text.Length);

            }

            int pre = max_label_length;

            max_label_length = Math.Max(max, Text.Length);

            labels[i].Text = Text;

            if (pre != max_label_length)
            {
                RevalidateLayout();
            }
        }

        public void Activate(params string[] arguments)
        {
            controller.Activate();

            controller.SetSelectedIndex(0);

            foreach(TextInput input in inputs)
            {
                input.Clear();
            }

            submit.Text = "OK";

            HandleArguments(arguments);
        }

        public void Deactivate()
        {
            controller.Deactivate();
        }

        public abstract void HandleArguments(string[] arguments);

        public void Initialize(RootContainer root)
        {

        }

        private int GetContentHeight()
        {
            int h_inputs = inputs.Length * 2 + 1;
            int h_button = 2;

            return h_inputs + h_button;
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
            OnFinish(this, new ReferenceArgs<Dictionary<string, object>>(new Dictionary<string, object> { ["continue"] = false }));
        }

        private void Continue_Option(object sender, ComponentEventArgs args)
        {
            OnFinish(this, new ReferenceArgs<Dictionary<string, object>>(new Dictionary<string, object> { ["continue"] = true }));
        }

    }

    public class TimeSlotSelectorContent : FormInputSelectorContent
    {
        public TimeSlotSelectorContent() :
            base("Month", "Week", "Day", "Slot")
        {
            Name = "TimeSlotSelector";

            Parsers["Month"] = ParseInt;
            Parsers["Week"] = ParseInt;
            Parsers["Day"] = ParseInt;
            Parsers["Slot"] = ParseInt;
        }

        private object ParseInt(string text, out bool valid)
        {
            valid = int.TryParse(text, out int number);

            if (valid)
            {
                return number;
            }
            else
            {
                return null;
            }
        }
        
        public override void HandleArguments(string[] arguments)
        {
            
        }
    }

    public class PersonSearchContent : FormInputSelectorContent
    {
        public PersonSearchContent():
            base("First Name", "Middle Initial", "Last Name", "Phone Number", "HCN")
        {
            Name = "PersonSearch";

            Parsers["First Name"] = ValidateNonEmpty;
            Parsers["Middle Initial"] = ValidateOneCharacter;
            Parsers["Last Name"] = ValidateNonEmpty;
            Parsers["Phone Number"] = ValidatePhoneNumber;
            Parsers["HCN"] = ValidateHCN;

            MinRequired = 1;
        }

        public override void HandleArguments(string[] arguments)
        {
        }

        private object ValidateNonEmpty(string text, out bool valid)
        {
            valid = text.Length > 0;

            return text;
        }

        private object ValidateOneCharacter(string text, out bool valid)
        {
            valid = text.Length == 1;

            return text;
        }

        private object ValidatePhoneNumber(string text, out bool valid)
        {
            valid = Regex.IsMatch(text, @"^(\+1)?\s*(\(\d{3}\)|\d{3})[\s-]+\d{3}[\s-]+\d{4}$");

            return text;
        }

        private object ValidateHCN(string text, out bool valid)
        {
            valid = Regex.IsMatch(text, @"^\d{10}\w{2}$");

            return text;
        }
    }

    public class PersonAddContent : FormInputSelectorContent
    {
        public PersonAddContent() :
            base("First Name", "Middle Initial", "Last Name", "Sex", "HCN")
        {
            Name = "PersonAddEntry";

            Parsers["First Name"] = ValidateNonEmpty;
            Parsers["Middle Initial"] = ValidateOneCharacter;
            Parsers["Last Name"] = ValidateNonEmpty;
            Parsers["Sex"] = ValidateSex;
            Parsers["HCN"] = ValidateHCN;
        }

        public override void HandleArguments(string[] arguments)
        {

        }

        private object ValidateNonEmpty(string text, out bool valid)
        {
            valid = text.Length > 0;

            return text;
        }

        private object ValidateOneCharacter(string text, out bool valid)
        {
            valid = text.Length == 1;

            return text;
        }

        private object ValidateSex(string text, out bool valid)
        {
            valid = text.Length == 1 &&
                Array.IndexOf(new char[] { 'M', 'F', 'I', 'H' },text[0]) != -1;

            return text;
        }

        private object ValidateHCN(string text, out bool valid)
        {
            valid = Regex.IsMatch(text, @"^\d{10}\w{2}$");

            return text;
        }
    }

    public class HouseDataInputContent : FormInputSelectorContent
    {
        public HouseDataInputContent() :
            base("Address Line 1", "Address Line 2", "City", "Province", "Phone Number", "Head Of House HCN")
        {
            Name = "HouseDataEntry";

            Parsers["Address Line 1"] = ValidateNonEmpty;
            Parsers["Address Line 2"] = ValidateAL2;
            Parsers["City"] = ValidateNonEmpty;
            Parsers["Province"] = ValidateNonEmpty;
            Parsers["Phone Number"] = ValidatePhoneNumber;
            Parsers["Head Of House HCN"] = ValidateHCN;

            MinRequired = 1;
        }

        public override void HandleArguments(string[] arguments)
        {

        }

        private object ValidateNonEmpty(string text, out bool valid)
        {
            valid = text.Length > 0;

            return text;
        }

        private object ValidateAL2(string text, out bool valid)
        {
            valid = true;

            return text;
        }

        private object ValidatePhoneNumber(string text, out bool valid)
        {
            valid = Regex.IsMatch(text, @"^(\+1)?\s*(\(\d{3}\)|\d{3})[\s-]+\d{3}[\s-]+\d{4}$");

            return text;
        }

        private object ValidateHCN(string text, out bool valid)
        {
            valid = Regex.IsMatch(text, @"^\d{10}\w{2}$");

            return text;
        }
    }

    public class MonthFilePathDataEntry: FormInputSelectorContent
    {
        public MonthFilePathDataEntry():
            base("Month", "File Path")
        {
            Name = "MonthFilePath";

            Parsers["Month"] = ParseMonth;
        }

        private object ParseMonth(string text, out bool valid)
        {
            valid = int.TryParse(text, out int number) && number >= 0 && number <= 11;

            if (valid)
            {
                return number;
            }
            else
            {
                return null;
            }
        }

        public override void HandleArguments(string[] arguments)
        {

        }
    }

}
