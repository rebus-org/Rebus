using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rebus.AzureServiceBus
{
    /// <summary>
    /// Retry helper that can be configured to accept certain exceptions (by specifying a function that returns whether the given
    /// exception is OK)
    /// </summary>
    public class Retrier
    {
        readonly List<Func<Exception, bool>> _exceptionAcceptors = new List<Func<Exception, bool>>();
        readonly List<TimeSpan> _waitTimeBetweenAttempts;

        /// <summary>
        /// Constructs the retrier, using the specified intervals between attempts
        /// </summary>
        public Retrier(IEnumerable<TimeSpan> waitTimeBetweenAttempts)
        {
            _waitTimeBetweenAttempts = waitTimeBetweenAttempts.ToList();
        }

        /// <summary>
        /// Sets up a function to check exceptions of type <typeparamref name="TException"/>, checking the caught
        /// exception with the acceptor function <paramref name="exceptionAcceptor"/>
        /// </summary>
        public Retrier On<TException>(Func<TException, bool> exceptionAcceptor) where TException : Exception
        {
            _exceptionAcceptors.Add(exception =>
            {
                if (!(exception is TException)) return false;

                return exceptionAcceptor((TException)exception);
            });

            return this;
        }

        /// <summary>
        /// Executes the specified function, retrying if necessary
        /// </summary>
        public async Task Execute(Func<Task> action)
        {
            var index = 0;
            var success = false;
            do
            {
                try
                {
                    await action();
                    success = true;
                    break;
                }
                catch (Exception exception)
                {
                    // if it's not an accepted exception, rethrow
                    if (!_exceptionAcceptors.Any(e => e(exception)))
                    {
                        throw;
                    }

                    // if we're out of attempts, rethrow
                    if (index >= _waitTimeBetweenAttempts.Count)
                    {
                        throw;
                    }

                    // otherwise....
                }

                await Task.Delay(_waitTimeBetweenAttempts[index++]);

            } while (!success);
        }
    }
}