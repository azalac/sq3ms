using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    /// An interface which represents content for the UI.
    /// </summary>
    public interface IInterfaceContent
    {
        /// <summary>
        /// This content's name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Called when this content finishes.
        /// </summary>
        event EventHandler<ReferenceArgs<Dictionary<string, object>>> Finish;

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
    /// Controls multiple <see cref="IInterfaceContent"/>s and handles their
    /// activation.
    /// </summary>
    public class InterfaceContentController
    {
        private Dictionary<string, IInterfaceContent> content = new Dictionary<string, IInterfaceContent>();

        public IInterfaceContent Current { get; private set; }

        public string Default { get; set; }

        public event EventHandler<ReferenceArgs<IInterfaceContent>> ContentChanged;

        public void Add(IInterfaceContent c)
        {
            content[c.Name] = c;
        }

        public IInterfaceContent Activate(string name)
        {
            if (!content.ContainsKey(name))
            {
                throw new ArgumentException("Interface Content '" + name + "' not registered");
            }

            if (Current != null)
            {
                Current.Deactivate();
            }

            Current = content[name];

            Current.Activate();

            if (ContentChanged != null)
            {
                ContentChanged(this, new ReferenceArgs<IInterfaceContent>(Current));
            }

            return Current;
        }

        public void Deactivate()
        {
            if (Current != null)
            {
                Current.Deactivate();
                Current = null;
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
    /// Controls the workflow between different <see cref="IInterfaceContent"/>, and validates their outputs.
    /// Invokes a delegate when the workflow is finished.
    /// </summary>
    public class InterfaceWorkflowController
    {

        /// <summary>
        /// Validates the output for a given workflow.
        /// </summary>
        /// <param name="output">The output for the workflow</param>
        /// <param name="error">An error message, or null for none</param>
        /// <returns><code>true</code> if the data is valid, <code>false</code> if the workflow controller should request a cancel.</returns>
        public delegate bool WorkflowValidator(Dictionary<string, object> output, out string error);

        /// <summary>
        /// Accepts the output when a workflow is completed.
        /// </summary>
        /// <param name="output">The output</param>
        public delegate void WorkflowDataAcceptor(Dictionary<string, object> output);



        /// <summary>
        /// A list of all workflows.
        /// </summary>
        private readonly Dictionary<string, Tuple<string[], WorkflowValidator[], WorkflowDataAcceptor>> workflows =
            new Dictionary<string, Tuple<string[], WorkflowValidator[], WorkflowDataAcceptor>>();

        private InterfaceContentController content_controller;



        /// <summary>
        /// The current workflow.
        /// </summary>
        private string current = null;

        /// <summary>
        /// The current workflow's stage.
        /// </summary>
        private int current_stage = 0;

        /// <summary>
        /// <code>true</code> when the last stage's data was invalid, and a cancel request was invoked.
        /// </summary>
        private bool requesting_cancel = false;

        /// <summary>
        /// The current workflow's accumulated values.
        /// </summary>
        private Dictionary<string, object> values = new Dictionary<string, object>();



        public InterfaceWorkflowController(InterfaceContentController content_controller)
        {
            this.content_controller = content_controller;
        }



        /// <summary>
        /// Adds a workflow to this controller.
        /// </summary>
        /// <param name="name">The name of the workflow</param>
        /// <param name="contentnames">A semi-colon separated list of content names to iterate over.</param>
        /// <param name="validators">The validators for the contents, or null if data should always be accepted.</param>
        public void AddWorkflow(string name, string contentnames, WorkflowDataAcceptor finish, params WorkflowValidator[] validators)
        {
            string[] split = contentnames.Split(';');

            if (validators == null)
            {
                validators = new WorkflowValidator[split.Length];
            }

            WorkflowValidator identity_validator = (Dictionary<string, object> output, out string error) => { error = ""; return true; };

            for (int i = 0; i < validators.Length; i++)
            {
                if (validators[i] == null)
                {
                    validators[i] = identity_validator;
                }
            }

            if (split.Length != validators.Length)
            {
                throw new ArgumentException("Workflow length and validators length do not match");
            }

            if (split.Length == 0)
            {
                throw new ArgumentException("Workflow must have atleast one content");
            }

            if (name == null || name.Length == 0)
            {
                throw new ArgumentException("Invalid workflow name");
            }

            workflows[name] = new Tuple<string[], WorkflowValidator[], WorkflowDataAcceptor>(split, validators, finish);
        }



        /// <summary>
        /// Invokes a workflow by name.
        /// </summary>
        /// <param name="name">The workflow</param>
        /// <param name="force">Whether the current workflow should be forcefully stopped or not.</param>
        public void InvokeWorkflow(string name, bool force = false)
        {
            if (current != null && !force)
            {
                throw new InvalidOperationException("Cannot invoke workflow: " + current + " is already active");
            }

            Reset();

            current = name;

            content_controller.Activate(workflows[name].Item1[0]);

            content_controller.Current.Finish += OnFinish;
        }

        /// <summary>
        /// Resets the workflow to the default.
        /// </summary>
        private void Reset()
        {
            current = null;
            current_stage = 0;
            values.Clear();

            content_controller.Deactivate();
        }

        /// <summary>
        /// Handles a cancel request finish event.
        /// </summary>
        /// <param name="args">The cancel menu's output</param>
        private void OnCancelRequestFinish(Dictionary<string, object> args)
        {
            requesting_cancel = false;

            bool cont = (bool)args["continue"];

            if (cont)
            {
                content_controller.Activate(workflows[current].Item1[current_stage]);

                content_controller.Current.Finish += OnFinish;
            }
            else
            {
                Reset();
            }
        }

        /// <summary>
        /// Handles a valid stage's output by proceeding to the next stage, or calling the data acceptor after the last stage.
        /// </summary>
        private void HandleValidStage()
        {
            Tuple<string[], WorkflowValidator[], WorkflowDataAcceptor> current_workflow = workflows[current];

            if (current_stage == current_workflow.Item1.Length - 1)
            {
                // if last stage, call the acceptor
                current_workflow.Item3(values);
                content_controller.Deactivate();
                Reset();
            }
            else
            {
                // otherwise, call the next stage
                current_stage++;
                content_controller.Activate(current_workflow.Item1[current_stage]);

                content_controller.Current.Finish += OnFinish;
            }
        }

        /// <summary>
        /// Handles an invalid stage's output by requesting a cancel.
        /// </summary>
        /// <param name="error_msg"></param>
        private void HandleInvalidStage(string error_msg)
        {
            IInterfaceContent content = content_controller.Activate("CancelRequest");

            content_controller.Current.Finish += OnFinish;

            if (content is CancelRequestController controller)
            {
                requesting_cancel = true;
                controller.Message = error_msg;
            }
            else
            {
                Debug.WriteLine("WARNING: RequestCancel in content controller isn't a CancelRequestController");
            }
        }

        /// <summary>
        /// Invoked when a content is finished. Validates the content's output, and moves to the next content if possible.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnFinish(object sender, ReferenceArgs<Dictionary<string, object>> args)
        {
            content_controller.Current.Finish -= OnFinish;

            // if a cancel was requested, handle the request and exit
            if (requesting_cancel)
            {
                OnCancelRequestFinish(args.Value);

                return;
            }

            Tuple<string[], WorkflowValidator[], WorkflowDataAcceptor> current_workflow = workflows[current];

            WorkflowValidator validator = current_workflow.Item2[current_stage];

            // if the output is valid,
            if (validator == null || validator(args.Value, out string error_msg))
            {
                // add the output to the values
                foreach (KeyValuePair<string, object> entry in args.Value)
                    values[entry.Key] = entry.Value;

                HandleValidStage();
            }
            else
            // if the output is invalid,
            {
                HandleInvalidStage(error_msg);
            }
        }

        /// <summary>
        /// Checks if the value in the dictionary equals the provided value.
        /// </summary>
        /// <typeparam name="T">The type to check for.</typeparam>
        /// <param name="dict">The dictionary.</param>
        /// <param name="option">The key for the dictionary.</param>
        /// <param name="value">The value to check against.</param>
        /// <returns>If the key doesn't exist, or if the types are incorrect, <code>false</code>, otherwise <see cref="object.Equals(object, object)"/> between
        /// The dictionary value and the provided value.</returns>
        public static bool CheckEquals<T>(Dictionary<string, object> dict, string option, T value)
        {
            if(!dict.ContainsKey(option))
            {
                return false;
            }

            object val = dict[option];

            if(typeof(T).IsAssignableFrom(val.GetType()))
            {
                return Equals((T)val, value);
            }
            else
            {
                return false;
            }
        }

    }

}
