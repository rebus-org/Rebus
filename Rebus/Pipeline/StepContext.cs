using System;
using System.Collections.Concurrent;
using Rebus.Transport;

namespace Rebus.Pipeline
{
    /// <summary>
    /// General step context model that encapsulates an object bag that can be used to pass data from step to step when executing a step pipeline
    /// </summary>
    public abstract class StepContext
    {
        /// <summary>
        /// Key reserved for the step context when it is inserted into the current <see cref="ITransactionContext.Items"/>
        /// </summary>
        public const string StepContextKey = "stepContext";

        readonly ConcurrentDictionary<string, object> _items = new ConcurrentDictionary<string, object>();

        /// <summary>
        /// Saves the given instance in the bag with a key derived from the (possibly explicitly specified) type <typeparamref name="T"/> (by calling <see cref="Type.FullName"/>).
        /// Any instances currently stored under that key will be overwritten.
        /// </summary>
        public T Save<T>(T instance)
        {
            return Save(typeof(T).FullName, instance);
        }

        /// <summary>
        /// Saves the given instance in the bag with the specified key. Any instances currently stored under that key will be overwritten.
        /// </summary>
        public T Save<T>(string key, T instance)
        {
            _items[key] = instance;
            return instance;
        }

        /// <summary>
        /// Loads the instance stored under the key that is stored under a key as determined by calling <see cref="Type.FullName"/> on the specified type <typeparamref name="T"/>.
        /// Returns null if none could be found.
        /// </summary>
        public T Load<T>()
        {
            return Load<T>(typeof(T).FullName);
        }

        /// <summary>
        /// Loads the instance stored under the specified key. Returns null if none could be found.
        /// </summary>
        public T Load<T>(string key)
        {
            object instance;
            return _items.TryGetValue(key, out instance)
                ? (T)instance
                : default(T);
        }
    }
}