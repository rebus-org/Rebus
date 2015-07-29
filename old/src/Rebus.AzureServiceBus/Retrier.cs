using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Rebus.AzureServiceBus
{
    class Retrier
    {
        readonly TimeSpan[] backoffTimes;
        readonly List<Type> toleratedExceptionTypes = new List<Type>();
        readonly List<Type> nonToleratedExceptionTypes = new List<Type>();
        readonly List<Action<Exception, TimeSpan, int>> retryExceptionCallbacks = new List<Action<Exception, TimeSpan, int>>();
        bool scanInnerExceptions;

        public Retrier(params TimeSpan[] backoffTimes)
        {
            this.backoffTimes = backoffTimes;
        }

        public Retrier TolerateInnerExceptionsAsWell()
        {
            scanInnerExceptions = true;
            return this;
        }

        public Retrier DoNotRetryOn<TException>() where TException : Exception
        {
            nonToleratedExceptionTypes.Add(typeof(TException));
            return this;
        }

        public Retrier RetryOn<TException>() where TException : Exception
        {
            toleratedExceptionTypes.Add(typeof(TException));
            return this;
        }

        public Retrier OnRetryException(Action<Exception, TimeSpan, int> exceptionCallback)
        {
            retryExceptionCallbacks.Add(exceptionCallback);
            return this;
        }

        public void Do(Action action)
        {
            var backoffIndex = 0;
            var complete = false;
            var caughtExceptions = new List<Timed<Exception>>();

            while (!complete)
            {
                try
                {
                    action();
                    complete = true;
                }
                catch (Exception e)
                {
                    caughtExceptions.Add(e.At(DateTime.Now));

                    if (backoffIndex >= backoffTimes.Length)
                    {
                        throw;
                    }

                    if (ExceptionCanBeTolerated(e))
                    {
                        var timeToSleep = backoffTimes[backoffIndex++];

                        retryExceptionCallbacks.ForEach(c => c(e, timeToSleep, backoffIndex));

                        Thread.Sleep(timeToSleep);
                    }
                    else
                    {
                        if (caughtExceptions.Count <= 1) throw;
                        
                        var message =
                            string.Format("Operation did not complete within {0} retries which resulted in exceptions at the following times: {1}",
                                backoffTimes.Length, String.Join(", ", caughtExceptions.Select(c => c.Time)));

                        throw new AggregateException(message, caughtExceptions.Select(c => c.Value));
                    }
                }
            }
        }

        bool ExceptionCanBeTolerated(Exception exceptionToCheck)
        {
            while (exceptionToCheck != null)
            {
                var exceptionType = exceptionToCheck.GetType();

                // non-tolerated exception types are concrete and cannot be tolerated
                if (nonToleratedExceptionTypes.Contains(exceptionType))
                {
                    return false;
                }

                // "toleratable" exceptions can be inherited - accept those as well
                if (toleratedExceptionTypes.Any(type => type.IsAssignableFrom(exceptionType)))
                {
                    return true;
                }

                // otherwise, see if we are allowed to check the inner exception as well
                exceptionToCheck = scanInnerExceptions
                    ? exceptionToCheck.InnerException
                    : null;
            }

            // exception could not be tolerated
            return false;
        }
    }
}