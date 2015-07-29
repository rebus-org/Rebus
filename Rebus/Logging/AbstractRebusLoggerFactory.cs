using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Rebus.Logging
{
    /// <summary>
    /// If you intend to implement your own logging, you probably want to derive
    /// from this class and implement <seealso cref="GetLogger"/>.
    /// </summary>
    public abstract class AbstractRebusLoggerFactory : IRebusLoggerFactory
    {
        /// <summary>
        /// Should get a logger for the specified type 
        /// </summary>
        protected abstract ILog GetLogger(Type type);

        /// <summary>
        /// Gets a logger that is initialized to somehow carry information on the class that is using it.
        /// Be warned that this method will most likely be pretty slow, because it will probably rely on
        /// some clunky <see cref="StackFrame"/> inspection.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public ILog GetCurrentClassLogger()
        {
            var stackFrame = new StackFrame(1);

            return GetLogger(stackFrame.GetMethod().DeclaringType);
        }
    }
}