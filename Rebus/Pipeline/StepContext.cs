using System.Collections.Concurrent;
using System.Threading;
using Rebus.Transport;

namespace Rebus.Pipeline;

/// <summary>
/// General step context model that encapsulates an object bag that can be used to pass data from step to step when executing a step pipeline
/// </summary>
public abstract class StepContext
{
    /// <summary>
    /// Key reserved for the step context when it is inserted into the current <see cref="ITransactionContext.Items"/>
    /// </summary>
    public const string StepContextKey = "stepContext";

    readonly ConcurrentDictionary<string, object> _items = new();
    readonly object[] _fastItems = new object[100];

    /// <summary>
    /// Saves the given instance in the bag with a key derived from the (possibly explicitly specified) type <typeparamref name="T"/>
    /// Any instances currently stored under that type will be overwritten.
    /// </summary>
    public T Save<T>(T instance)
    {
        _fastItems[TypedId<T>.Id] = instance;
        return instance;
    }

    /// <summary>
    /// Loads the instance stored under the type <typeparamref name="T"/> using it as a key.
    /// Returns null if none could be found.
    /// </summary>
    public T Load<T>() => (T)_fastItems[TypedId<T>.Id];

    /// <summary>
    /// Saves the given instance in the bag with the specified key. Any instances currently stored under that key will be overwritten.
    /// </summary>
    public T Save<T>(string key, T instance)
    {
        _items[key] = instance;
        return instance;
    }

    /// <summary>
    /// Loads the instance stored under the specified key. Returns null if none could be found.
    /// </summary>
    public T Load<T>(string key) => _items.TryGetValue(key, out var instance) ? (T)instance : default(T);

    /// <summary>
    /// Index-per-type counter that is incremented every time accessing <see cref="TypedId{TKey}"/> causes a new type to be generated
    /// </summary>
    static int _index;

    /// <summary>
    /// Static type with static ID field that can automatically work as a type-to-index mapping
    /// </summary>
    // ReSharper disable once UnusedTypeParameter
    static class TypedId<TKey>
    {
        // ReSharper disable once StaticMemberInGenericType
        internal static readonly int Id = Interlocked.Increment(ref _index);
    }
}