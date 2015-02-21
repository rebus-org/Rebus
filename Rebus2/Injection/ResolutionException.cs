using System;

namespace Rebus2.Injection
{
    public class ResolutionException : Exception
    {
        public ResolutionException(string message, params object[] objs)
            : base(string.Format(message, objs))
        {
        }

        public ResolutionException(Exception innerException, string message, params object[] objs)
            : base(string.Format(message, objs), innerException)
        {
        }
    }
}