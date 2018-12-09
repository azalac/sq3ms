using System;
using System.Threading;

namespace SchedulingUI
{
    /// <summary>
    /// This class handles all keyboard input, and broadcasts it through the
    /// KeyPress event. It has an internal thread which must be started before
    /// it will work.
    /// </summary>
	public class KeyboardInput
	{
        /// <summary>
        /// This delay is the forced delay between keys. It was supposed to
        /// act as a 'debounce' for buttons, but it didn't work. 50 is a good
        /// value, but this isn't needed anymore.
        /// Setting it to >0 effectively flushes the input, only accepting one 
        /// key at a time, and discarding others.
        /// </summary>
        private static long KEY_DELAY = 5;

        private RootContainer Container;

        /// <summary>
        /// The internal thread. public reference is so that outer classes can
        /// manipulate the thread as needed (join, interrupt, etc).
        /// </summary>
		public Thread InternalThread { get; private set;}

        /// <summary>
        /// The key which stops the internal thread.
        /// </summary>
		public ConsoleKey ExitKey { get; set; }

		private bool running = true;

		public KeyboardInput(RootContainer root)
		{
			if (root == null) {
				throw new ArgumentException ("RootContainer cannot be null", "root");
			}

			Container = root;
		}

        /// <summary>
        /// Initializes an starts the internal thread
        /// </summary>
		public void StartThread()
		{
			InternalThread = new Thread (new ThreadStart (this.Run)) {
				Name = "KeyboardInput-Thread",
				IsBackground = true
			};

			InternalThread.Start ();
		}

        /// <summary>
        /// Shuts down the internal thread. Only effective once a key is pressed.
        /// </summary>
		public void Shutdown()
		{
			running = false;
		}

		private void Run()
		{
            double last_key_time = 0;

            DateTime UNIX_EPOCH = new DateTime(1970, 1, 1);

			while (running)
			{

				ConsoleKeyInfo key = Console.ReadKey (true);

                DebugLog.LogComponent("Handling KEY: " + key.Key);

				if (Container != null)
				{
                    double current_millis = (DateTime.UtcNow - UNIX_EPOCH).TotalMilliseconds;
                    if (current_millis - last_key_time > KEY_DELAY)
                    {
                        Container.OnKeyPressed(this, new ConsoleKeyEventArgs(key));
                        last_key_time = current_millis;
                    }
				}
				else
				{
					DebugLog.LogComponent ("Warning: RootContainer is null");
				}

				if (ExitKey != 0 && key.Key == ExitKey)
				{
					running = false;
				}

			}
		}

	}
}

