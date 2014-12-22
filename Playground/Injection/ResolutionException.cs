using System;

namespace Playground.Injection
{
    public class ResolutionException : Exception
    {
        public ResolutionException(string message, params object[] objs)
            : base(string.Format(message, objs))
        {
        }
    }
}