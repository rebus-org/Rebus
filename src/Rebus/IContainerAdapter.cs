using System;

namespace Rebus
{
    /// <summary>
    /// Extends a handler activator with the ability to register stuff.
    /// </summary>
    public interface IContainerAdapter : IActivateHandlers
    {
        /// <summary>
        /// Registers the given instance as an implementation of the specified service types.
        /// </summary>
        void RegisterInstance(object instance, params Type[] serviceTypes);
        
        /// <summary>
        /// Registers a mapping from the given service types to the specfied implementation type.
        /// If no service types are specified, the implementation is registered as an implementation
        /// of itself.
        /// </summary>
        void Register(Type implementationType, Lifestyle lifestyle, params Type[] serviceTypes);
        
        /// <summary>
        /// Should return whether the container contains something that claims to implement the specified
        /// service type.
        /// </summary>
        bool HasImplementationOf(Type serviceType);
        
        /// <summary>
        /// Gets an instance of the specified service type. Lifestyle is managed by the container.
        /// </summary>
        TService Resolve<TService>();

        /// <summary>
        /// Gets instances of all implementations of the given service type. Lifestyle is managed
        /// by the container.
        /// </summary>
        TService[] ResolveAll<TService>();
        
        /// <summary>
        /// Returns control of the given instance to the container.
        /// </summary>
        void Release(object obj);
    }
}