using System;

namespace Rebus
{
    /// <summary>
    /// Extends a handler activator with the ability to register stuff.
    /// </summary>
    public interface IContainerAdapter : IActivateHandlers
    {
        void RegisterInstance(object instance, params Type[] serviceTypes);
        bool HasImplementationOf(Type serviceType);
        IStartableBus GetStartableBus();
    }
}