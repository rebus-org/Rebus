using Rebus.Snoop.ViewModel.Models;

namespace Rebus.Snoop.Events
{
    public class MessageDeleted
    {
        readonly Message messageThatWasDeleted;

        public MessageDeleted(Message messageThatWasDeleted)
        {
            this.messageThatWasDeleted = messageThatWasDeleted;
        }

        public Message MessageThatWasDeleted
        {
            get { return messageThatWasDeleted; }
        }
    }
}