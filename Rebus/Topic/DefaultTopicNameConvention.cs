using System;
using Rebus.Extensions;

namespace Rebus.Topic
{
    /// <summary>
    /// Default convention to name topics
    /// </summary>
    public class DefaultTopicNameConvention : ITopicNameConvention
    {
        /// <summary>
        /// Returns the default topic name based on type of message
        /// </summary>
        public string GetTopic(Type eventType)
        {
            return eventType.GetSimpleAssemblyQualifiedName();
        }
    }
}