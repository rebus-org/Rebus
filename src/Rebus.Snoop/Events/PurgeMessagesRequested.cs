using Rebus.Snoop.ViewModel.Models;

namespace Rebus.Snoop.Events
{
    public class PurgeMessagesRequested
    {
        readonly Queue queue;

        public PurgeMessagesRequested(Queue queue)
        {
            this.queue = queue;
        }

        public Queue Queue
        {
            get { return queue; }
        }
    }
}