using System.Collections.Generic;

namespace Rebus.Injection
{
    /// <summary>
    /// Represents the context of resolving one root service and can be used throughout the tree to fetch something to be injected
    /// </summary>
    public interface IResolutionContext
    {
        /// <summary>
        /// Gets an instance of the specified <typeparamref name="TService"/>.
        /// </summary>
        TService Get<TService>();

        /// <summary>
        /// Gets all instances resolved within this resolution context at this time.
        /// </summary>
        IEnumerable<T> GetTrackedInstancesOf<T>();
    }
}