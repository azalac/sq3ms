/*
 * FILE: EventQueue.cs
 * 
 * PROJECT: WMP A2
 * 
 * PROGRAMMER: Austin Zalac 7900020
 * 
 * FIRST VERSION: 1/10/18
 * 
 * DESCRIPTION:
 *  The EventQueue class allows modules to invoke events in a separate thread,
 *      which abstracts away the location and implementation of the events.
 *  
 *  The Events class contains the events for this project, which are contained
 *      in static variables.
 *  
 *  The EventQueueUtils class has a single method, which converts an object to
 *      the specified type, or throws an InvalidParameterException if the
 *      object is invalid.
 *  
 *  The InvalidParameterException extends the Exception base class but has no
 *      special features.
 */


using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace EventQueueStuff
{
    
    /// <summary>
    /// This was originally supposed to be an enum, but because enums are
    /// values, it caused a lot of problems. Contains all valid events for the
    /// event queue.
    /// </summary>
    public class Events
    {
        #region Event Constants
        /// <summary>
        /// Invoked when a new document should be created.
        /// </summary>
        public static readonly Events NEW_DOC = new Events("NEW_DOC");

        /// <summary>
        /// Invoked when a file should be opened. Accepts a string parameter.
        /// </summary>
        public static readonly Events OPEN_FILE = new Events("OPEN_FILE");

        /// <summary>
        /// Invoked when the document should be saved.
        /// </summary>
        public static readonly Events SAVE_DOC = new Events("SAVE_DOC");

        /// <summary>
        /// Usually invoked as a callback for CONFIRM_DELETE. If the parameter
        /// is false, the document gets saved. Accepts a bool parameter.
        /// </summary>
        public static readonly Events SAVE_IF = new Events("SAVE_CALLBACK");

        /// <summary>
        /// Invoked when the document's path should be updated. Does not call
        /// OPEN_FILE. Used with SAVE_DOC. If the path is invalid, throws an
        /// exception. Accepts a string parameter.
        /// </summary>
        public static readonly Events UPDATE_PATH = new Events("UPDATE_PATH");
        
        /// <summary>
        /// Invoked when the user should be prompted about deleting their
        /// changes. Accepts no parameters. Returns true if the user wants to
        /// erase their changes.
        /// </summary>
        public static readonly Events CONFIRM_DELETE = new Events("CONFIRM_DELETE");

        /// <summary>
        /// Invoked when the user should be prompted about a file's save
        /// location. Accepts no parameters. Returns the path as a string, or
        /// null if none was selected.
        /// </summary>
        public static readonly Events REQUEST_SAVE_FILE = new Events("REQ_SAVE_FILE");

        #endregion


        private string Name;

        /// <summary>
        /// Private in order to emulate an enum.
        /// </summary>
        /// <param name="name">The event's name</param>
        private Events(string name)
        {
            this.Name = name;
        }

        /// <summary>
        /// Helper for debugging to see which event this is.
        /// </summary>
        /// <returns>The event's name</returns>
        public override string ToString()
        {
            return Name;
        }
    }
    
    /// <summary>
    /// This class is used as a connector between the modules for this project.
    /// </summary>
    /// <typeparam name="T">The type to use for events</typeparam>
    public class EventQueue<T> where T : class
    {

        /// <summary>
        /// The list of queued events.
        /// </summary>
        private BlockingCollection<IEventExecutor> event_queue = new BlockingCollection<IEventExecutor>();
        
        /// <summary>
        /// Locks which prevent an event from being queued twice w/o handling
        /// the first event.
        /// </summary>
        private Dictionary<IEventExecutor, object> evt_locks = new Dictionary<IEventExecutor, object>();

        /// <summary>
        /// The event handlers
        /// </summary>
        private Dictionary<T, Adapter<object, object>> handlers =
			new Dictionary<T, Adapter<object, object>>();
        
        #region Handler Register Methods

        /// <summary>
        /// Registers a handler for an event which takes no parameters and
        /// returns void.
        /// </summary>
        /// <param name="evt">The event</param>
        /// <param name="handler">The parameter</param>
        public void RegisterEventHandler(T evt, Action handler)
        {
            handlers[evt] = x => { handler(); return null; };
        }

        /// <summary>
        /// Registers a handler for an event which takes one parameter and
        /// returns void.
        /// </summary>
        /// <param name="evt">The event</param>
        /// <param name="handler">The parameter</param>
        public void RegisterEventHandler_In(T evt, Action<object> handler)
        {
            handlers[evt] = x => { handler(x); return null; };
        }

        /// <summary>
        /// Registers a handler for an event which takes no parameters and
        /// returns an object.
        /// </summary>
        /// <param name="evt">The event</param>
        /// <param name="handler">The parameter</param>
        public void RegisterEventHandler_Out(T evt, Func<object> handler)
        {
            handlers[evt] = x => handler();
        }

        /// <summary>
        /// Registers a handler for an event which takes one parameter and
        /// returns an object.
        /// </summary>
        /// <param name="evt">The event</param>
        /// <param name="handler">The parameter</param>
		public void RegisterEventHandler_InOut(T evt, Adapter<object, object> handler)
        {
            handlers[evt] = handler;
        }

        #endregion

        #region Queueing Methods

        /// <summary>
        /// Queues an event to be ran.
        /// </summary>
        /// <param name="e">The event</param>
        public void QueueEvent(T e)
        {
            QueueEvent(e, null);
        }

        /// <summary>
        /// Queues an event to be ran with a parameter.
        /// </summary>
        /// <param name="e">The event</param>
        /// <param name="param">The parameter</param>
        public void QueueEvent(T e, object param)
        {
            QueueEvent(new SimpleEventExecutor<T> { Argument = param, Event = e, Handlers = handlers });
        }
        
        public void QueueComplexEvent(ComplexEvent<T> Event, Dictionary<string, Tuple<object, string>> context)
        {
            QueueEvent(new ComplexEventExecutor<T> { Event = Event, Context = context, Handlers = handlers });
        }

        public void QueueEvent(IEventExecutor executor)
        {
            WaitUntilHandled(executor);

            event_queue.Add(executor);
        }

        /// <summary>
        /// Waits until the given event is handled, if it is already queued.
        /// </summary>
        /// <param name="e"></param>
        private void WaitUntilHandled(IEventExecutor e)
        {
            // if the event is queued, wait until it is handled
            if (event_queue.Any(evt => e.Equals(evt)))
            {
                lock (evt_locks[e])
                {
                    Monitor.Wait(evt_locks[e]);
                }
            }
        }

        #endregion
        
        /// <summary>
        /// De-queues an event and executes it. Blocks infinitely.
        /// </summary>
        public void Run()
        {
            while (true)
            {
                IEventExecutor evt = event_queue.Take();

                // error handling
                if (evt == null)
                {
                    continue;
                }

                // make sure this event has a lock
                if (!evt_locks.ContainsKey(evt))
                {
                    evt_locks[evt] = new object();
                }

                evt.Execute();

                // alert a waiting queue-thread
                lock(evt_locks[evt])
                {
                    Monitor.Pulse(evt_locks[evt]);
                }

            }
        }
        
    }

    /// <summary>
    /// Syntax:
    /// '||' - On fail
    /// '&&' - On succeed
    /// ';' - And then
    /// '->' - On succeed, chained
    /// 'Event(name)' - event with parameter (context name)
    /// </summary>
    public class ComplexEvent<T> where T : class
    {

        private List<EventInfo<T>> Events = new List<EventInfo<T>>();

        public int Count { get { return Events.Count; } }

        public ComplexEvent(string events, Func<string, T> eventLookup)
        {
            StringBuilder str = new StringBuilder(events);

            EventInfo<T> current = new EventInfo<T>();
            
            current.Condition = EventCondition.ANY;

            while (str.Length > 0)
            {
                string token = NextToken(str);
                
                // if a new event is found, add the current one to the list
                if (ValidCondition(token) && current.HasEvent)
                {

					// only special events can be chained with an argument
					if (current.SpecialType != SpecialEvent.SET)
					{
						if (current.Argument != null && current.Condition == EventCondition.CHAIN)
						{
							throw new ArgumentException ("Events (" + current.Event + ") cannot be chained with an argument");
						}
					}

					if (current.SpecialType == SpecialEvent.SET)
					{
						if (current.ArgType != ArgumentTypes.PROPERTY)
						{
							throw new ArgumentException ("A set event must have a property argument");
						}
					}

                    Events.Add(current);

                    current = new EventInfo<T>();
                }

                // get the event's condition
                switch (token)
                {
                    case "||":
                        current.Condition = EventCondition.FAIL;
                        continue;

                    case "&&":
                        current.Condition = EventCondition.SUCCEED;
                        continue;

                    case ";":
                        current.Condition = EventCondition.ANY;
                        continue;

                    case "->":
                        current.Condition = EventCondition.CHAIN;
                        continue;

                }

				// is a special event name
				if (token.ToLower () == "set" && !current.HasEvent)
				{
					current.SpecialType = SpecialEvent.SET;
					current.EventName = token;
					continue;
				}

                // is a non-special event name
                if (!current.HasEvent)
                {
                    current.Event = eventLookup(token);
					current.EventName = token;
                    continue;
                }

                // is an argument
                if (token == "(")
                {
					ArgumentTypes type;
                    current.Argument = ParseArgument(NextToken(str), out type);

                    current.ArgType = type;

                    string after = NextToken(str);

                    if (after != ")")
                    {
                        throw new ArgumentException("Invalid Token: Expected ')', got '" + after + "' in argument definition");
                    }

                    continue;

                }

                throw new ArgumentException("Invalid Token '" + token + "'");

            }

            if(current.Condition != EventCondition.UNDEFINED && !current.HasEvent)
            {
                throw new ArgumentException("Found condition at end of event");
            }

            Events.Add(current);

        }

		public void Debug_DumpEvents()
		{
			Events.ForEach(e => Console.WriteLine(e));
		}

        public bool ShouldExecute(int index, bool prev_succeed)
        {
            switch(Events[index].Condition)
            {
                case EventCondition.ANY:
                case EventCondition.UNDEFINED:
                    return true;

                case EventCondition.SUCCEED:
                case EventCondition.CHAIN:
                    return prev_succeed;

                case EventCondition.FAIL:
                    return !prev_succeed;
            }

            throw new ArgumentException("Found enum value with no rule in ComplexEvent<T>.ShouldExecute()");
        }

        public T this[int index]
        {
            get { return Events[index].Event; }
        }
        
        public object GetArgument(int index, object previous,
            Dictionary<string, Tuple<object, string>> context)
        {
            EventInfo<T> e = Events[index];

            if(e.Condition == EventCondition.CHAIN)
            {
                return previous;
            }
            
			if(e.ArgType == ArgumentTypes.PROPERTY)
            {
                // is property
                Tuple<object, string> t = context[(string)e.Argument];
                return GetProperty(t.Item1, t.Item2);
            }
            else
            {
                // is literal
                return e.Argument;
            }

        }

		public bool IsSpecial(int index)
		{
			return Events [index].SpecialType != SpecialEvent.UNDEFINED;
		}

		public object HandleSpecial(int index, object previous,
			Dictionary<string, Tuple<object, string>> context)
		{
			switch (Events [index].SpecialType) {
			case SpecialEvent.SET:
				if (Events [index].ArgType != ArgumentTypes.PROPERTY) {
					throw new ArgumentException (
						"A set event's argument isn't a property, this should never be thrown");
				}

				HandleSetEvent (previous, context [(string)Events [index].Argument]);

				break;
			}

			return null;
		}

		private void HandleSetEvent(object value, Tuple<object, string> property)
		{
			System.Reflection.PropertyInfo real_property =
				property.Item1.GetType().GetProperty(property.Item2);

			if (value.GetType ().IsAssignableFrom (real_property.PropertyType))
			{
				System.Diagnostics.Debug.WriteLine ("Warning: {0}'s type ({1}) does " +
					"not match provided value's type ({2})",
				                                   property.Item2, real_property.PropertyType,
				                                   value.GetType ());
				throw new EventFailException ();
			}

			real_property.SetValue (property.Item1, value);
		}

        private static object GetProperty(object obj, string prop)
        {
            System.Reflection.PropertyInfo property = obj.GetType().GetProperty(prop);
            
			if (property == null) {
				return null;
			}

            return property.GetValue(obj, null);
        }

		public string GetEventName(int index)
		{
			return Events [index].EventName;
		}

        #region Parsing

        private static bool ValidCondition(string str)
        {
            return str == "||" || str == "&&" || str == ";" || str == "->";
        }
        
        private static string NextToken(StringBuilder str)
        {
            int i1 = 0;
            
            while(i1 < str.Length && Char.IsWhiteSpace(str[i1]))
            {
                i1++;
            }

            int i2 = i1;

            int cat = GetCharacterCategory(str[i2]);

            while(i2 < str.Length && GetCharacterCategory(str[i2]) == cat)
            {
                i2++;
            }
            
            string str2 = str.ToString(i1, i2 - i1);

            str.Remove(0, i2);

            return str2;

        }

		/// <summary>
		/// Determines a character's category when parsing.
		/// The actual values this function returns aren't important.
		/// The only thing that matters is that they're unique.
		/// </summary>
		/// <returns>The characer's category.</returns>
		/// <param name="c">The character.</param>
        private static int GetCharacterCategory(char c)
        {
            if (c == '(' || c == ')')
            {
                return 3;
            }
            if (c == '|')
            {
                return 4;
            }
            if (c == '&')
            {
                return 5;
            }
            if (c == ';')
            {
                return 6;
            }
            if (c == '-' || c == '>')
            {
                return 7;
            }
            if (Char.IsDigit(c) || c == '.')
            {
                return 1;
            }
            if(Char.IsLetter(c) || c == '\'')
            {
                return 2;
            }
            return 0;
        }

		private static object ParseArgument(string arg, out ArgumentTypes type)
        {
            // string literal
            if(arg.StartsWith("'") && arg.EndsWith("'"))
            {
				type = ArgumentTypes.STRING;
                return arg.Substring(1, arg.Length - 2);
            }

            // integer literal
            if(arg.All(Char.IsDigit))
            {
				type = ArgumentTypes.INTEGER;
                return int.Parse(arg);
            }

            // decimal literal
            if(arg.All(c => Char.IsDigit(c) || c == '.'))
			{
				type = ArgumentTypes.FLOAT;
                return double.Parse(arg);
            }

            // null
            if(arg == "null")
            {
				type = ArgumentTypes.NULL;
                return null;
            }

			type = ArgumentTypes.PROPERTY;
            return arg;

        }
        #endregion
    }

    class EventInfo<T> where T: class
    {
        public T Event { get; set; }

		public string EventName { get; set; }

        public EventCondition Condition { get; set; }

        public object Argument { get; set; }
		public ArgumentTypes ArgType { get; set; }

		public SpecialEvent SpecialType { get; set; }

		public bool HasEvent { 
			get {
				return Event != null || SpecialType != SpecialEvent.UNDEFINED;
			}
		}

        public override string ToString()
        {
            return "Event{" + EventName + ", " + Argument + ", " + Condition + "}";
        }
    }

    enum EventCondition
    {
        UNDEFINED,
        SUCCEED,
        FAIL,
        ANY,
        CHAIN
    }

	enum ArgumentTypes
	{
		NONE,
		STRING,
		INTEGER,
		FLOAT,
		NULL,
		PROPERTY
	}

	enum SpecialEvent
	{
		UNDEFINED,
		SET
	}

	#region Event Executors

    public interface IEventExecutor
    {
        object Lock { get; }

        void Execute();
    }

    public class SimpleEventExecutor<T> : IEventExecutor where T: class
    {
        public T Event { get; set; }

        public object Argument { get; set; }
        
		public Dictionary<T, Adapter<object, object>> Handlers { get; set; }

        public object Lock {
			get{ return Event;}
		}

        public void Execute()
        {
            if (!Handlers.ContainsKey(Event))
            {
                throw new ArgumentException("Event " + Event + " has no registered handler");
            }

            Handlers[Event](Argument);
        }
    }

    public class ComplexEventExecutor<T> : IEventExecutor where T : class
    {
        public ComplexEvent<T> Event { get; set; }

        public Dictionary<string, Tuple<object, string>> Context { get; set; }

		public Dictionary<T, Adapter<object, object>> Handlers { get; set; }

		public static bool DebugPrint { get; set; }

		public ComplexEventExecutor()
		{
			DebugPrint = false;
		}

        public object Lock
		{
			get{return Event[0];}
		}

        public void Execute()
        {
            object arg = null;
            bool prev_ran = true;

            for(int i = 0; i < Event.Count; i++)
            {
                if (Event.ShouldExecute(i, prev_ran))
                {

                    try
                    {
						
						if (Event.IsSpecial (i))
						{
							arg = Event.HandleSpecial (i, arg, Context);
						}
						else
						{
                        	T evt = Event[i];
								
                        	if (!Handlers.ContainsKey(evt))
                        	{
                        	    throw new ArgumentException("Event " + evt + " has no registered handler");
                        	}
                        	
                        	arg = Handlers[evt](Event.GetArgument(i, arg, Context));
                        	prev_ran = true;
						}
                    }
                    catch (EventFailException)
                    {
                        prev_ran = false;
						System.Diagnostics.Debug.WriteLineIf(DebugPrint,
							"Event '" + Event.GetEventName(i) + "' failed");
                    }
                }
                else
                {
                    prev_ran = false;
                }
            }
        }
        
    }

	#endregion

    /// <summary>
    /// A helper class to make using an event queue easier.
    /// </summary>
    public class EventQueueUtils
    {

        /// <summary>
        /// Casts an event handler's parameter to a usable object, and throws
        /// an InvalidParameterException if the types are incompatible.
        /// </summary>
        /// <typeparam name="P">The type to cast to</typeparam>
        /// <param name="param">The parameter to cast</param>
        /// <returns>The casted object</returns>
        public static P TryParamCast<P>(object param)
        {
            if (param is P)
            {
                return (P)param;
            }
            else
            {
                throw new InvalidParameterException(param);
            }
        }


    }
	
	/// <summary>
	/// The interface which all handlers get converted to.
	/// </summary>
	/// <typeparam name="P">The input type</typeparam>
	/// <typeparam name="R">The output type</typeparam>
	/// <param name="t">The input parameter</param>
	/// <returns>The output object</returns>
	public delegate R Adapter<in P, out R>(P t);

    /// <summary>
    /// An exception class which is thrown from the EventQueueUtils.TryParamCast
    /// function.
    /// </summary>
    public class InvalidParameterException : Exception
    {
        public InvalidParameterException(object param):
            base(string.Format("Invalid Event Parameter \'" + param + "\'"))
        {
        }
    }

    /// <summary>
    /// Thrown when an event fails.
    /// </summary>
    public class EventFailException: Exception
    {

    }
}
