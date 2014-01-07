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

        public Retrier RetryOn<TException>() where TException : Exception
        {
            toleratedExceptionTypes.Add(typeof(TException));
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
                        Thread.Sleep(backoffTimes[backoffIndex++]);
                    }
                    else
                    {
                        throw new AggregateException(String.Format("Operation did not complete within {0} retries which resulted in exceptions at the following times: {1}",
                            backoffTimes.Length, String.Join(", ", caughtExceptions.Select(c => c.Time))), caughtExceptions.Select(c => c.Value));
                    }
                }
            }
        }

        bool ExceptionCanBeTolerated(Exception exceptionToCheck)
        {
            while (exceptionToCheck != null)
            {
                var exceptionType = exceptionToCheck.GetType();

                // if the exception can be tolerated...
                if (toleratedExceptionTypes.Contains(exceptionType))
                {
                    return true;
                }

                // otherwise, see if we are allowed to check the inner exception as well
                exceptionToCheck = scanInnerExceptions
                    ? exceptionToCheck.InnerException
                    : null;
            }

            // exception cannot be tolerated
            return false;
        }
    }
}