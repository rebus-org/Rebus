namespace Rebus.Bus
{
    /// <summary>
    /// Implement this and install an instance in order to hook into Rebus' unit of work. The unit of work
    /// will be created right after the message context has been established, thus allowing the unit
    /// of work to access the context.
    /// </summary>
    public interface IUnitOfWorkManager
    {
        /// <summary>
        /// Return an instance of your implementation of <see cref="IUnitOfWork"/>
        /// </summary>
        IUnitOfWork Create();
    }
}