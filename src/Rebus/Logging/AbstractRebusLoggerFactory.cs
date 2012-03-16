using System;
using System.Diagnostics;

namespace Rebus.Logging
{
    /// <summary>
    /// If you intend to implement your own logging, you probably want to derive
    /// from this class and implement <seealso cref="GetLogger"/>.
    /// </summary>
    public abstract class AbstractRebusLoggerFactory : IRebusLoggerFactory
    {
        protected abstract ILog GetLogger(Type type);

        public ILog GetCurrentClassLogger()
        {
            var stackFrame = new StackFrame(1);

            return GetLogger(stackFrame.GetMethod().DeclaringType);
        }
    }
}