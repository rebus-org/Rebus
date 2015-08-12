using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace Rebus
{
    /// <summary>
    /// Extensions useful for doing special stuff on exceptions.
    /// </summary>
    public static class ExceptionExtensions
    {
        /// <summary>
        /// Modifies the specified exception's _remoteStackTraceString. I have no idea how this works, but it allows 
        /// for unpacking a re-throwable inner exception from a caught <see cref="TargetInvocationException"/>.
        /// Read <see cref="http://stackoverflow.com/a/2085364/6560"/> for more information.
        /// </summary>
        public static void PreserveStackTrace(this Exception exception)
        {
            try
            {
                var ctx = new StreamingContext(StreamingContextStates.CrossAppDomain);
                var mgr = new ObjectManager(null, ctx);
                var si = new SerializationInfo(exception.GetType(), new FormatterConverter());

                exception.GetObjectData(si, ctx);
                mgr.RegisterObject(exception, 1, si); // prepare for SetObjectData
                mgr.DoFixups(); // ObjectManager calls SetObjectData

                // voila, exception is unmodified save for _remoteStackTraceString
            }
            catch (Exception ex)
            {
                var message = string.Format("This exception was caught while attempting to preserve the stack trace for" +
                                            " an exception: {0} - the original exception is passed as the inner exception" +
                                            " of this exception. This is most likely caused by the absence of a proper" +
                                            " serialization constructor on an exception", ex);

                throw new ApplicationException(message, exception);
            }
        }
    }
}