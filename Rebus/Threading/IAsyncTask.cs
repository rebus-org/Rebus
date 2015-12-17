using System;

namespace Rebus.Threading
{
    /// <summary>
    /// 
    /// </summary>
    public interface IAsyncTask : IDisposable
    {
        /// <summary>
        /// Starts the task
        /// </summary>
        void Start();
    }
}