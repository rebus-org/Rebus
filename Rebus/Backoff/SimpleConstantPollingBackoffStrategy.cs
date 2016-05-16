using System.Threading.Tasks;
using Rebus.Workers;

namespace Rebus.Backoff
{
    /// <summary>
    /// Simple constant polling strategy
    /// </summary>
    public class SimpleConstantPollingBackoffStrategy : IBackoffStrategy
    {
        /// <summary>
        /// Asynchronously executes the next wait operation, possibly advancing the wait cursor to a different wait time for the next time
        /// </summary>
        public Task Wait()
        {
            return Task.Delay(200);
        }

        /// <summary>
        /// Asynchronously waits a while when an error has occurred
        /// </summary>
        public Task WaitError()
        {
            return Task.Delay(5000);
        }

        /// <summary>
        /// Resets the wait cursor, ensuring that the next wait operation will start over
        /// </summary>
        public void Reset()
        {
        }
    }
}