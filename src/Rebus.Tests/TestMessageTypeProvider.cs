using System;

namespace Rebus.Tests
{
    /// <summary>
    /// Message type provider that allows all types used in tests to be serialized.
    /// </summary>
    public class TestMessageTypeProvider : IProvideMessageTypes
    {
        public Type[] GetMessageTypes()
        {
            return new[] {typeof (string)};
        }
    }
}