using Rebus.Snoop.ViewModel.Models;

namespace Rebus.Snoop.Events
{
    public class UpdateMessageRequested
    {
        readonly Message message;

        public UpdateMessageRequested(Message message)
        {
            this.message = message;
        }

        public Message Message
        {
            get { return message; }
        }
    }
}