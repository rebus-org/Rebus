using System.Threading.Tasks;

namespace Rebus.Config
{
    /// <summary>
    /// Contains additional options for configuring Rebus internals
    /// </summary>
    public class Options
    {
        /// <summary>
        /// This is the default number of workers that will be started, unless <see cref="NumberOfWorkers"/> is set to something else
        /// </summary>
        public const int DefaultNumberOfWorkers = 2;

        /// <summary>
        /// This is the default number of concurrent asynchrounous operations allowed FOR EACH WORKER, unless <see cref="MaxParallelism"/> is set
        /// to something else
        /// </summary>
        public const int DefaultMaxParallelism = 5;

        /// <summary>
        /// Constructs the options with the default settings
        /// </summary>
        public Options()
        {
            NumberOfWorkers = DefaultNumberOfWorkers;
            MaxParallelism = DefaultMaxParallelism;
        }

        /// <summary>
        /// Configures the number of workers
        /// </summary>
        public int NumberOfWorkers { get; set; }

        /// <summary>
        /// Configures how many outstanding continuations (i.e. async <see cref="Task"/>-based parallel operations) we allow per worker
        /// </summary>
        public int MaxParallelism { get; set; }
    }
}