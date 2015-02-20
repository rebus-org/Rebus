using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rebus2.Pipeline
{
    public interface IStep
    {
        Task Process(StepContext context, Func<Task> next);
    }

    public class StepContext
    {
        readonly Dictionary<string, object> _items = new Dictionary<string, object>();

        public T Save<T>(T instance)
        {
            _items[typeof (T).FullName] = instance;
            return instance;
        }

        public T Load<T>()
        {
            object instance;
            return _items.TryGetValue(typeof (T).FullName, out instance)
                ? (T) instance
                : default(T);
        }
    }

}