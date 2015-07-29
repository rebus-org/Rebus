using Rebus.Snoop.ViewModel.Models;

namespace Rebus.Snoop.Events
{
    public class MessageMoved
    {
        readonly Message messageThatWasMoved;
        readonly string sourceQueuePath;
        readonly string destinationQueuePath;
        readonly bool copyWasLeftInSourceQueue;

        public MessageMoved(Message messageThatWasMoved, string sourceQueuePath, string destinationQueuePath, bool copyWasLeftInSourceQueue)
        {
            this.messageThatWasMoved = messageThatWasMoved;
            this.sourceQueuePath = sourceQueuePath;
            this.destinationQueuePath = destinationQueuePath;
            this.copyWasLeftInSourceQueue = copyWasLeftInSourceQueue;
        }

        public bool CopyWasLeftInSourceQueue
        {
            get { return copyWasLeftInSourceQueue; }
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