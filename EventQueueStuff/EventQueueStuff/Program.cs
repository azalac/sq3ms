using System;
using System.Collections.Generic;
using System.Threading;

namespace EventQueueStuff
{
    class Program
    {
        public int AProperty { get; set; }

		public Type AnotherProperty { get; set; }

        static void Main(string[] args)
        {
            Program p = new Program();

			p.AProperty = 5;

			p.Run ();

        }

        public void Run()
		{

			//executes 'prod', then chains the output to 'typeof', then chains the output to 'print'
			//runs 'func' if print is successful
			//the x => x is a mapper between a string and the event type object
			//this event uses strings as the events, which is a bad idea because there's no
			//restrictions on what a string could be.
			ComplexEvent<string> evt = new ComplexEvent<string> ("prod -> typeof -> print && func", x => x);

			//executes 'prod', then chains the output to 'typeof', then chains to  set, which sets 'Into'
			//set is a special pre-defined event which allows you to get values out of event chains.
			//do not register an event called set, because it will never get called.
			ComplexEvent<string> evt2 = new ComplexEvent<string> ("prod -> typeof -> set(Into)", x => x);

			//executes 'failfunc', which always fails. If 'failfunc' passed, "passed" gets printed
			//if 'failfunc' fails, "failed" gets printed. "finished" always gets printed.
			ComplexEvent<string> evt3 = new ComplexEvent<string> (
				"failfunc && print('passed') || print('failed'); print('finished')", x => x);



			EventQueue<string> q = new EventQueue<string> ();

			// A producer takes nothing and returns an object
			// is registered with '..._Out'
			q.RegisterEventHandler_Out ("prod", Producer);

			// An adapter takes an object and returns an object
			// is registered with '..._InOut'
			q.RegisterEventHandler_InOut ("typeof", Adapter);

			// A consumer takes an object and returns nothing
			// is registers with '..._In'
			q.RegisterEventHandler_In ("print", Consumer);

			// A function takes nothing and returns nothing
			// is registered with no suffix
			q.RegisterEventHandler ("func", Function);

			// Another function which always fails
			q.RegisterEventHandler ("failfunc", FailingFunction);

			// Note: events always accept and return objects internally, so
			// they can be used in any order. If a handler returns nothing,
			// the event actually returns null. Events will always accept an
			// argument, but handlers which take no arguments won't get the
			// argument.



			// create a thread for the event queue to run in
			Thread t = new Thread (new ThreadStart (q.Run)) {
				IsBackground = true,
				Name = "EventQueue"
			};
			t.Start ();



			// create a context for the event to run with
			// I will probably turn this into an object named 'EventContext' or something
			Dictionary<string, Tuple<object, string>> context = new Dictionary<string, Tuple<object, string>> ();

			// associates the argument 'Value' with this object's 'AProperty' property
			context ["Value"] = new Tuple<object, string> (this, "AProperty");

			// associates the argument 'Into' with this object's 'AnotherProperty' property
			context ["Into"] = new Tuple<object, string> (this, "AnotherProperty");




			Console.WriteLine ("Queueing first event");

			// queue the event
			q.QueueComplexEvent (evt, context);

			// wait for it to finish
			Console.WriteLine ("Press any key to continue...");
			Console.ReadKey (true);
			Console.WriteLine ("\n\n");




			Console.WriteLine ("Queueing second event");
			
			Console.WriteLine ("Value of AnotherProperty:" + AnotherProperty);


			// queue the second event
			q.QueueComplexEvent (evt2, context);


			// wait for it to finish
			Console.WriteLine ("Press any key to continue...");
			Console.ReadKey (true);

			Console.WriteLine ("Value of AnotherProperty:" + AnotherProperty);

			Console.WriteLine ("\n\n");




			Console.WriteLine ("Queueing third event");

			// queue the third event
			q.QueueComplexEvent (evt3, context);

			// wait for it to finish
			Console.WriteLine ("Press any key to continue...");
			Console.ReadKey (true);
			Console.WriteLine ("\n\n");

			
			// Because the event queue runs in a second thread, we can never be sure of when
			// events get ran. That's why the 'func' event is sometimes written before 'Press any key to finish...' 
			// That's what a race condition is, if you've heard of the term before.

			Console.WriteLine ("Queueing simple events");

			// queue a simple event
			q.QueueEvent ("func");

			// queue a simple event with an argument
			q.QueueEvent ("print", "Hello World");

			Console.WriteLine ("Press any key to finish...");
			Console.ReadKey (true);
		}

        private object Producer()
        {
            Console.WriteLine("Producer");

            return "Hello world";
        }

		private object Adapter(object arg)
		{
			Console.WriteLine ("Adapter");
			return arg.GetType ();
		}

		private void Consumer(object arg)
		{
			Console.WriteLine ("Consumer: '" + arg + "'");
		}

        private void Function()
        {
            Console.WriteLine("Function");
        }

		private void FailingFunction()
		{
			Console.WriteLine ("FailingFunction");

			throw new EventFailException ();
		}


    }
}
