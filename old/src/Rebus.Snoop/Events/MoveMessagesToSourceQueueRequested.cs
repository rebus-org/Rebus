using System.Collections.Generic;
using Rebus.Snoop.ViewModel.Models;

namespace Rebus.Snoop.Events
{
    public class MoveMessagesToSourceQueueRequested
    {
        public MoveMessagesToSourceQueueRequested(List<Message> messagesToMove)
        {
            this.messagesToMove = messagesToMove;
        }

        public List<Message> MessagesToMove
        {
            get { return messagesToMove; }
        }

        readonly List<Message> messagesToMove;
    }
}