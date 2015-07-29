namespace Rebus.Configuration
{
    /// <summary>
    /// Extends <see cref="IActivateHandlers"/> into a container adapter,
    /// which has the capability of storing the created bus instance(s)
    /// and disposing it/them at the right time - usually when the application
    /// ends (i.e. when the underlying IoC container is disposed).
    /// </summary>
    public interface IContainerAdapter : IActivateHandlers
    {
        /// <summary>
        /// Instructs the container to save the specified bus instance 
        /// and take responsibility of their disposal when it's the right time.
        /// </summary>
        void SaveBusInstances(IBus bus);
    }
}