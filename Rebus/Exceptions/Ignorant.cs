using System;
using System.Linq;
using Rebus.Time;

namespace Rebus.Exceptions
{
    /// <summary>
    /// Helps keeping track of errors that we want to ignore for a while before acknowledging them
    /// </summary>
    public class Ignorant
    {
        readonly Func<Exception, string> _exceptionToEqualityKeyMapper;

        volatile string _currentKey;
        DateTimeOffset _time;
        int _silencePeriodIndex;

        /// <summary>
        /// Gets/sets the silence periods, i.e. the length of the intervals in which the ignorant will be silent
        /// </summary>
        public TimeSpan[] SilencePeriods { get; set; }

        /// <summary>
        /// Constructs the ignorant with the given mapping from an exception instance to the equality key that will be used to compare with previous exceptions.
        /// If no equality key mapper is given, it will default to using <see cref="Type.FullName"/> of the exception
        /// </summary>
        public Ignorant(Func<Exception, string> exceptionToEqualityKeyMapper = null)
        {
            _exceptionToEqualityKeyMapper = exceptionToEqualityKeyMapper ?? (exception => exception.GetType().FullName);

            SilencePeriods = new[]
            {
                TimeSpan.FromMinutes(1),
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(10),
            };
        }

        /// <summary>
        /// Checks whether the given exception is to be ignored
        /// </summary>
        public bool IsToBeIgnored(Exception exception)
        {
            var key = _exceptionToEqualityKeyMapper(exception);

            var now = RebusTime.Now;

            if (!string.Equals(_currentKey, key))
            {
                _currentKey = key;
                _time = now;
                
                return true;
            }

            var silencePeriod = GetCurrentSilencePeriod();

            if ((now - _time) > silencePeriod)
            {
                // double-check locking to have only one thread pass on the exception
                lock (this)
                {
                    if ((now - _time) > silencePeriod)
                    {
                        _time = now;
                        _silencePeriodIndex++;
                        
                        return false;
                    }
                }
            }

            return true;
        }

        TimeSpan GetCurrentSilencePeriod()
        {
            var currentIndex = _silencePeriodIndex;

            return currentIndex >= SilencePeriods.Length
                ? SilencePeriods.LastOrDefault()
                : SilencePeriods[currentIndex];
        }

        /// <summary>
        /// Resets the silence period tracker - should be called after each success
        /// </summary>
        public void Reset()
        {
            _currentKey = null;
            _silencePeriodIndex = 0;
        }
    }
}