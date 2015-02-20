using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus2.Messages;

namespace Rebus2.Pipeline
{
    public interface IStep
    {
        Task Process(StepContext context, Func<Task> next);
    }

    public class StepContext
    {
        readonly Dictionary<string, object> _items = new Dictionary<string, object>();

        public StepContext(TransportMessage receivedTransportMessage)
        {
            Save(receivedTransportMessage);
        }

        public T Save<T>(T instance)
        {
            return Save(typeof (T).FullName, instance);
        }

        public T Save<T>(string key, T instance)
        {
            _items[key] = instance;
            return instance;
        }

        public T Load<T>()
        {
            return Load<T>(typeof (T).FullName);
        }

        public T Load<T>(string key)
        {
            object instance;
            return _items.TryGetValue(key, out instance)
                ? (T) instance
                : default(T);
        }
    }

}