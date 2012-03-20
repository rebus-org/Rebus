using Rebus.Snoop.ViewModel.Models;

namespace Rebus.Snoop.Events
{
    public class MessageMoved
    {
        readonly Message messageThatWasMoved;
        readonly string sourceQueuePath;
        readonly string destinationQueuePath;

        public MessageMoved(Message messageThatWasMoved, string sourceQueuePath, string destinationQueuePath)
        {
            this.messageThatWasMoved = messageThatWasMoved;
            this.sourceQueuePath = sourceQueuePath;
            this.destinationQueuePath = destinationQueuePath;
        }

        public Message MessageThatWasMoved
        {
            get { return messageThatWasMoved; }
        }

        public string SourceQueuePath
        {
            get { return sourceQueuePath; }
        }

        public string DestinationQueuePath
        {
            get { return destinationQueuePath; }
        }
    }
}