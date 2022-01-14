using System;

namespace Rebus.Topic;

/// <summary>
/// Defines the rules to name topics
/// </summary>
public interface ITopicNameConvention
{
    /// <summary>
    /// Returns the topic name based on type of message
    /// </summary>
    string GetTopic(Type eventType);
}