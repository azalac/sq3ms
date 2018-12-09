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
        void Activate(params string[] arguments);

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

        public IInterfaceContent Activate(string name, params string[] arguments)
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

            Current.Activate(arguments);

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

}
