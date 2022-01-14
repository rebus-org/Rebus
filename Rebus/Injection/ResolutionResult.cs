using System;
using System.Collections;

namespace Rebus.Injection;

/// <summary>
/// Contains a built object instance along with all the objects that were used to build the instance
/// </summary>
public class ResolutionResult<TService>
{
    internal ResolutionResult(TService instance, IEnumerable trackedInstances)
    {
        if (instance == null) throw new ArgumentNullException(nameof(instance));
        Instance = instance;
        TrackedInstances = trackedInstances ?? throw new ArgumentNullException(nameof(trackedInstances));
    }

    /// <summary>
    /// Gets the instance that was built
    /// </summary>
    public TService Instance { get; }

    /// <summary>
    /// Gets all object instances that were used to build <see cref="Instance"/>, including the instance itself
    /// </summary>
    public IEnumerable TrackedInstances { get; }
}