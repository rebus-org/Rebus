using System;

namespace Rebus.Bus
{
    /// <summary>
    /// Implement this in your own flavor to let Rebus control how your transaction is comitted/aborted.
    /// If your commit fails, and that requires <seealso cref="Abort"/> to be called, please do it yourself.
    /// Also, if you're using multiple units of work, please note that Rebus can't take any kinds of responsibility
    /// of what happens in the event that the last commit fails. 
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// Implement your commit logic here. Your commit logic should throw in the event that it cannot do its thing.
        /// </summary>
        void Commit();
        
        /// <summary>
        /// Implement your rollback logic here. If your rollback logic must be called in the event that the commit fails,
        /// you must do so yourself.
        /// </summary>
        void Abort();
    }
}