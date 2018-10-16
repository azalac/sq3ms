using System;
using System.Threading;

namespace SchedulingUI
{
	public class KeyboardInput
	{

		public RootContainer Container { get; private set; }

		public Thread InternalThread { get; private set;}

		public ConsoleKey ExitKey { get; set; }

		private bool running = true;

		public KeyboardInput(RootContainer root)
		{
			if (root == null) {
				throw new ArgumentException ("RootContainer cannot be null", "root");
			}

			Container = root;
		}

		public void StartThread()
		{
			InternalThread = new Thread (new ThreadStart (this.Run)) {
				Name = "KeyboardInput-Thread",
				IsBackground = true
			};

			InternalThread.Start ();
		}

		public void Shutdown()
		{
			running = false;
		}

		private void Run()
		{
			while (running)
			{

				ConsoleKeyInfo key = Console.ReadKey (true);

				if (Container != null)
				{
					Container.OnKeyPress (this, key);
				}
				else
				{
					System.Diagnostics.Debug.WriteLine ("Warning: RootContainer is null");
				}

				if (ExitKey != 0 && key.Key == ExitKey)
				{
					running = false;
				}

			}
		}

	}
}

