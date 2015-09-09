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
        public async Task Wait()
        {
            await Task.Delay(200);
        }

        /// <summary>
        /// Resets the wait cursor, ensuring that the next wait operation will start over
        /// </summary>
        public void Reset()
        {
            // no logic in here yet ;)
        }
    }
}