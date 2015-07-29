using System.Threading.Tasks;

namespace Rebus.Workers.ThreadBased
{
    /// <summary>
    /// Helper thingie that can help with implementing a backoff strategy, e.g. exponential or something else. Pretty crude for now.
    /// </summary>
    public class BackoffHelper
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