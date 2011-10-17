namespace Rebus.Messages
{
    /// <summary>
    /// Wrapper of a raw message
    /// </summary>
    public class RawMessage
    {
        public RawMessage(string id, object theRawMessage)
        {
            Id = id;
            TheRawMessage = theRawMessage;
        }

        public string Id { get; set; }
        public object TheRawMessage { get; set; }
    }
}