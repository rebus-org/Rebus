using System.Collections.Generic;
using Rebus.Snoop.ViewModel.Models;

namespace Rebus.Snoop.Events
{
    public class CopyMessagesToQueueRequested
    {
        readonly List<Message> messagesToMove;

        public CopyMessagesToQueueRequested(List<Message> messagesToMove)
        {
            this.messagesToMove = messagesToMove;
        }

        public List<Message> MessagesToMove
        {
            get { return messagesToMove; }
        }
    }
}