using System;
using System.Runtime.Serialization;

namespace Playground
{
    [Serializable]
    public class ResolutionException : Exception
    {
        public ResolutionException(string message, params object[] objs)
            : base(string.Format(message, objs))
        {
        }

        public ResolutionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}