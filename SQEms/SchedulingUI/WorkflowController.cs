/*
* FILE          : WorkflowController.cs
* PROJECT       : INFO-2180 Software Quality 1, Term Project
* PROGRAMMER    : Austin Zalac
* FIRST VERSION : November 4, 2018
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulingUI
{

    /// <summary>
    /// Validates the output for a given workflow.
    /// </summary>
    /// <param name="output">The output for the workflow</param>
    /// <param name="error">An error message, or null for none</param>
    /// <returns><code>true</code> if the data is valid, <code>false</code> if the workflow controller should request a cancel.</returns>
    public delegate bool WorkflowValidator(Dictionary<string, object> output, out string error);

    /// <summary>
    /// Gets the redirected workflow for a given stage.
    /// </summary>
    /// <param name="current_stage">The current stage.</param>
    /// <param name="stage_name">The current stage's name.</param>
    /// <param name="valid">The current stage output's validity</param>
    /// <param name="output">The current stage's output</param>
    /// <returns>The name of the redirected workflow, or null for no redirect</returns>
    public delegate string WorkflowRedirector(int current_stage, string stage_name, bool valid, Dictionary<string, object> output);

    /// <summary>
    /// Accepts the output when a workflow is completed.
    /// </summary>
    /// <param name="output">The output</param>
    /// <returns>The data to merge with the previous workflow</returns>
    public delegate Dictionary<string, object> WorkflowDataAcceptor(Dictionary<string, object> output);

    public enum WorkflowState
    {
        RUNNING,
        WAITING_FOR_REDIRECTS,

        FINISHED
    }

    public struct Workflow
    {
        public string[] stages;
        public List<string>[] arguments;
        public WorkflowValidator[] validators;
        public WorkflowDataAcceptor acceptor;
        public WorkflowRedirector redirector;
    }

    public class WorkflowInstance
    {
        public string name = null;
        public int stage = 0;
        public WorkflowState state = WorkflowState.RUNNING;
        public Dictionary<string, string> arguments = new Dictionary<string, string>();
        public Dictionary<string, object> values = new Dictionary<string, object>();
        public Dictionary<string, object> completed = new Dictionary<string, object>();
    }


    /// <summary>
    /// Controls the workflow between different <see cref="IInterfaceContent"/>, and validates their outputs.
    /// Invokes a delegate when the workflow is finished.
    /// </summary>
    public class InterfaceWorkflowController
    {
        /// <summary>
        /// A data acceptor which does nothing.
        /// </summary>
        public static readonly WorkflowDataAcceptor IDENTITY_ACCEPTOR = (output) => null;

        /// <summary>
        /// A redirector which does nothing.
        /// </summary>
        public static readonly WorkflowRedirector IDENTITY_REDIRECT = (stage, name, valid, output) => null;

        /// <summary>
        /// A validator which does nothing.
        /// </summary>
        public static readonly WorkflowValidator IDENTITY_VALIDATOR = (Dictionary<string, object> output, out string error) => { error = ""; return true; };




        /// <summary>
        /// A list of all workflows.
        /// </summary>
        private readonly Dictionary<string, Workflow> workflows = new Dictionary<string, Workflow>();

        private InterfaceContentController content_controller;


        /// <summary>
        /// The current workflow.
        /// </summary>
        private WorkflowInstance current;


        /// <summary>
        /// A stack of held-workflows.
        /// A held-workflow is a workflow that is in progress, but is not completed.
        /// This is usually used when the workflow has an optional menu.
        /// </summary>
        private Stack<WorkflowInstance> held_flows = new Stack<WorkflowInstance>();
        
        public InterfaceWorkflowController(InterfaceContentController content_controller, string cancelcontent = "CancelRequest(error)")
        {
            this.content_controller = content_controller;

            AddWorkflow("__CancelRequest", cancelcontent, OnCancelRequestFinish);
        }

        #region Workflow Management

        /// <summary>
        /// Adds a workflow to this controller.
        /// </summary>
        /// <remarks>
        /// Format for the content names string:
        /// 
        /// stage/content name(dynamic arg, :const arg);another stage(args, ...)
        /// 
        /// A name, referring to the IInterfaceContent name, followed by optional
        /// argument list (which are passed to the content's Activate method).
        /// dynamic arguments are specified when the workflow is invoked, via
        /// a 'workflow(arg1, arg2, ...)' format, and constant arguments are
        /// specified in the workflow, by preceeding the argument name by a colon.
        /// The argument name is then used as the argument, so ':text' would be
        /// passed as 'text', regardless if ':text' is in the argument dictionary.
        /// Arguments starting with an exlamation mark with reference the current
        /// values, rather than the arguments.
        /// </remarks>
        /// <param name="name">The name of the workflow</param>
        /// <param name="contentnames">A semi-colon separated list of content names to iterate over.</param>
        /// <param name="validators">The validators for the contents, or null if data should always be accepted.</param>
        public void AddWorkflow(string name, string contentnames, WorkflowDataAcceptor finish, WorkflowRedirector redirector = null, params WorkflowValidator[] validators)
        {
            Tuple<string[], List<List<string>>> flow = ParseWorkflow(contentnames);

            // if the validators weren't specified, create them
            if (validators == null || validators.Length == 0)
            {
                validators = new WorkflowValidator[flow.Item1.Length];
            }

            // fill the validators with the identity version if they're null
            for (int i = 0; i < validators.Length; i++)
            {
                if (validators[i] == null)
                {
                    validators[i] = IDENTITY_VALIDATOR;
                }
            }

            // ensure there's a correct number of validators
            if (flow.Item1.Length != validators.Length)
            {
                throw new ArgumentException("Workflow length and validators length do not match");
            }

            // ensure there's atleast one stage
            if (flow.Item1.Length == 0)
            {
                throw new ArgumentException("Workflow must have atleast one content");
            }

            // ensure the name is valid
            if (name == null || name.Length == 0)
            {
                throw new ArgumentException("Invalid workflow name");
            }

            Workflow workflow = new Workflow
            {
                stages = flow.Item1,
                arguments = flow.Item2.ToArray(),
                validators = validators,
                acceptor = finish,
                redirector = redirector
            };

            workflows[name] = workflow;
        }

        /// <summary>
        /// Parses a workflow string into the required datastructure.
        /// </summary>
        /// <param name="input">The workflow string.</param>
        /// <returns>An array maintaining the order of the stages, and a dictionary with the stage names as keys,
        /// and arguments as values.</returns>
        private Tuple<string[], List<List<string>>> ParseWorkflow(string input)
        {
            List<List<string>> workflow = new List<List<string>>();
            List<string> order = new List<string>();

            foreach (string chunk in input.Split(';'))
            {
                if(chunk.Length == 0)
                {
                    continue;
                }

                List<string> args = ParseStageName(chunk, out string name);

                order.Add(name);

                workflow.Add(args);
            }

            return new Tuple<string[], List<List<string>>>(order.ToArray(), workflow);
        }


        /// <summary>
        /// Parses a stage name chunk into a nice-to-use datastructure.
        /// </summary>
        /// <param name="input">The name chunk.</param>
        /// <param name="name">The stage's name.</param>
        /// <returns>The stage's arguments.</returns>
        private List<string> ParseStageName(string input, out string name)
        {
            input = input.Trim();

            if (input.EndsWith(")") && input.Contains("("))
            {
                string[] sections = input.Substring(0, input.Length - 1).Split('(');

                name = sections[0];

                return new List<string>(sections[1].Split(',').Select(s => s.Trim()));
            }
            else
            {
                name = input;

                return null;
            }
        }





        /// <summary>
        /// Invokes a workflow by name.
        /// </summary>
        /// <param name="name">The workflow</param>
        /// <param name="force">Whether the current workflow should be forcefully stopped if running.</param>
        public void InvokeWorkflow(string name, bool force = false)
        {
            if (current != null && !force)
            {
                throw new InvalidOperationException("Cannot invoke workflow: " + current.name + " is already active");
            }

            current = new WorkflowInstance();
            
            current.arguments = ParseWorkflowName(name, out current.name, current.values);
            
            StartStage(attachevent: true);
            
            DebugLog.LogController("Invoking workflow '" + name + "'");
        }
        
        /// <summary>
        /// Parses a workflow invocation into nice-to-use datastructures.
        /// </summary>
        /// <remarks>
        /// The format for this is:
        /// name(key=value, key=value, ...)
        /// </remarks>
        /// <param name="input">The input string.</param>
        /// <param name="name">The workflow name.</param>
        /// <returns>The workflow arguments.</returns>
        private Dictionary<string, string> ParseWorkflowName(string input, out string name, Dictionary<string, object> values)
        {
            input = input.TrimEnd();
            
            // if the input matches '...(...)'
            if(input.EndsWith(")") && input.Contains("("))
            {
                string[] sections = input.Substring(0, input.Length - 1).Split('(');

                name = sections[0].Trim();

                Dictionary<string, string> args = new Dictionary<string, string>();

                foreach(string arg in sections[1].Split(','))
                {
                    string[] halves = arg.Split('=');

                    halves[0] = halves[0].Trim();

                    if (halves[0].StartsWith("&"))
                    {
                        values[halves[0].Substring(1)] = halves[1].Trim();
                    }
                    else
                    {
                        args[halves[0]] = halves[1].Trim();
                    }
                }

                return args;
            }
            else
            {
                name = input;

                return null;
            }
        }


        #endregion

        #region Stage Control


        /// <summary>
        /// Resolves the argument values for a given stage by getting its
        /// required arguments and attempting to get them from the current
        /// invocation's arguments. Arguments preceeded by a colon are taken
        /// as a literal constant, rather than an index to the argument dictionary.
        /// Arguments starting with an exlamation mark with reference the current
        /// values.
        /// </summary>
        /// <param name="stage">The stage.</param>
        /// <param name="default_value">The default value if the argument couldn't be found.</param>
        /// <returns>The arguments.</returns>
        private string[] ResolveArguments(int stage = -1, string default_value = "")
        {
            if(stage == -1)
            {
                stage = current.stage;
            }

            Workflow c = workflows[current.name];
            List<string> args = c.arguments[stage];
            
            if(args == null)
            {
                return null;
            }

            string[] resolved = new string[args.Count];

            for(int i = 0; i < resolved.Length; i++)
            {
                string arg = args[i];

                if (arg.StartsWith(":"))
                {
                    resolved[i] = arg.Substring(1);
                }
                else if (arg.StartsWith("!"))
                {
                    resolved[i] = current.values[arg.Substring(1)].ToString();
                }
                else if (!current.arguments.TryGetValue(args[i], out resolved[i]))
                {
                    resolved[i] = default_value;
                }
            }

            return resolved;
        }

        /// <summary>
        /// Starts a stage.
        /// </summary>
        /// <param name="workflow">The workflow to start from - defaults to current.</param>
        /// <param name="stage">The stage to start - defaults to current.</param>
        /// <param name="set">If the workflow and state should be updated.</param>
        /// <param name="attachevent">If an OnFinish handler should be attached to the stage.</param>
        private void StartStage(string workflow = null, int stage = -1, bool set = false, bool attachevent = false)
        {
            if (workflow == null)
            {
                workflow = current.name;
            }

            if (stage == -1)
            {
                stage = current.stage;
            }

            string[] args = ResolveArguments(stage);
            content_controller.Activate(workflows[workflow].stages[stage], args);

            DebugLog.LogController("Displaying stage " + workflows[workflow].stages[stage]);

            if (set)
            {
                current = new WorkflowInstance()
                {
                    name = workflow,
                    stage = stage
                };
            }

            if (attachevent)
            {
                content_controller.Current.Finish += OnFinish;
            }

        }



        /// <summary>
        /// Resets the workflow to the default.
        /// </summary>
        private void Reset()
        {
            current = null;
        }



        /// <summary>
        /// Pushes the current workflow into the held workflows, but does not reset anything.
        /// </summary>
        private void HoldWorkflow()
        {
            held_flows.Push(current);

            DebugLog.LogController("Holding workflow '" + current.name + "' - stack size: " + held_flows.Count);
        }

        /// <summary>
        /// Restores the previous held workflow.
        /// </summary>
        private void RestoreWorkflow()
        {
            WorkflowInstance old = current;

            current = held_flows.Pop();

            if (old.completed != null)
            {
                foreach (var kvp in old.completed)
                {
                    current.values[kvp.Key] = kvp.Value;
                }
            }

            if(current.stage == workflows[current.name].stages.Length)
            {
                HandleWorkflowExit();
                return;
            }

            if (current != null)
            {
                StartStage(attachevent: true);
            }

            DebugLog.LogController("Restoring workflow '" + current.name + "'");
        }

        #endregion

        #region Event Handling

        /// <summary>
        /// Handles a cancel request finish event.
        /// </summary>
        /// <param name="args">The cancel menu's output</param>
        private Dictionary<string, object> OnCancelRequestFinish(Dictionary<string, object> args)
        {
            if (CheckEquals(args, "continue", false))
            {
                // removes the previous workflow if it was invalid
                held_flows.Pop();
            }

            return null;
        }
        
        private void HandleWorkflowExit()
        {
            string name = current.stage < workflows[current.name].stages.Length ?
                workflows[current.name].stages[current.stage] : null;

            current.completed = workflows[current.name].acceptor(current.values);
            
            DebugLog.LogController(string.Format("Stage {0}/{1}:{3} is exiting ({2})", name, current.stage, DictToString(current.completed), current.name));
            
            // if there's a held workflow, restore it
            if (held_flows.Count > 0)
            {
                DebugLog.LogController("Restoring");
                RestoreWorkflow();
            }
            else
            {
                DebugLog.LogController("Resetting");
                // otherwise, reset to default screen
                Reset();

                content_controller.Deactivate();
            }
        }

        private string DictToString<K, V>(Dictionary<K, V> dict)
        {
            if(dict == null)
            {
                return "{null}";
            }

            return "{" + string.Join(", ", dict.Select(kvp => kvp.Key + "=" + kvp.Value)) + "}";
        }

        /// <summary>
        /// Invoked when a content is finished. Validates the content's output, and moves to the next content if possible.
        /// </summary>
        private void OnFinish(object sender, ReferenceArgs<Dictionary<string, object>> args)
        {
            content_controller.Current.Finish -= OnFinish;

            string error = null;
            bool? valid = workflows[current.name].validators[current.stage]?.Invoke(args.Value, out error);

            string name = current.stage < workflows[current.name].stages.Length ?
                workflows[current.name].stages[current.stage] : null;

            string redirect = null;

            if (valid.Value || !valid.HasValue)
            {
                DebugLog.LogController(string.Format("Stage {0}/{1} has valid output ({2})", name, current.stage, DictToString(args.Value)));

                foreach (KeyValuePair<string, object> entry in args.Value)
                    current.values[entry.Key] = entry.Value;

                redirect = workflows[current.name].redirector?.Invoke(current.stage,
                    name, valid ?? true, current.values);

                current.stage++;


                if (current.stage < workflows[current.name].stages.Length)
                {
                    StartStage(attachevent: true);
                }
            }
            else
            {
                DebugLog.LogController(string.Format("Stage {0}/{1} has invalid output ({2})", name, current.stage, DictToString(args.Value)));

                HoldWorkflow();

                InvokeWorkflow("__CancelRequest(error = " + error + ")", true);

                return;
            }

            if(redirect != null)
            {
                DebugLog.LogController(string.Format("Stage {0}/{1} is redirecting to {2}", name, current.stage, redirect));

                HoldWorkflow();

                InvokeWorkflow(redirect, true);

                return;
            }
            else
            {
                DebugLog.LogController(string.Format("Stage {0}/{1} is not redirecting", name, current.stage));
            }

            if (current.stage == workflows[current.name].stages.Length)
            {
                HandleWorkflowExit();
            }
        }
        
        #endregion

        /// <summary>
        /// Checks if the value in the dictionary equals the provided value.
        /// </summary>
        /// <typeparam name="T">The type to check for.</typeparam>
        /// <param name="dict">The dictionary.</param>
        /// <param name="option">The key for the dictionary.</param>
        /// <param name="value">The value to check against.</param>
        /// <returns>If the key doesn't exist, or if the types are incorrect, <code>false</code>, otherwise <see cref="object.Equals(object, object)"/> between
        /// the dictionary value and the provided value.</returns>
        public static bool CheckEquals<T>(Dictionary<string, object> dict, string option, T value)
        {
            if (!dict.ContainsKey(option))
            {
                return false;
            }

            object val = dict[option];

            if(val == null || value == null)
            {
                return val == null && value == null;
            }

            if (typeof(T).IsAssignableFrom(val.GetType()))
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
