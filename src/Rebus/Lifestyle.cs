namespace Rebus
{
    /// <summary>
    /// Basic lifestyles that a given container implementation must support.
    /// </summary>
    public enum Lifestyle
    {
        /// <summary>
        /// The component is created the first time it is requested, subsequent requests must yield the same instance.
        /// </summary>
        Singleton,

        /// <summary>
        /// The component is created each time it is requested. It may, however - and that is up to you and the container
        /// of your choice - be scoped to something, like e.g. an aggregating implementation of <see cref="IHandleMessages{TMessage}"/>
        /// or something similar.
        /// </summary>
        Instance,
    }
}