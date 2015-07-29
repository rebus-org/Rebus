using Rebus.Snoop.ViewModel.Models;

namespace Rebus.Snoop.Events
{
    public class UpdateMessageRequested
    {
        readonly Message message;
        readonly Queue queue;

        public UpdateMessageRequested(Message message, Queue queue)
        {
            this.message = message;
            this.queue = queue;
        }

        public Message Message
        {
            get { return message; }
        }

        public Queue Queue
        {
            get { return queue; }
        }
    }
}