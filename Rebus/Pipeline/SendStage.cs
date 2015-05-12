namespace Rebus.Pipeline
{
    /// <summary>
    /// Indicates some known stages within a send pipeline, allowing for <see cref="IOutgoingStep"/> implementations to position themselves correctly without necessarily
    /// knowing anything about any other steps
    /// </summary>
    public enum SendStage
    {
        /// <summary>
        /// There's no known stages yet ;)
        /// </summary>
        None = 1000
    }
}