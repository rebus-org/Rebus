using Rebus.Snoop.ViewModel.Models;

namespace Rebus.Snoop.Events
{
    public class QueueDeleted
    {
        readonly Queue queue;

        public QueueDeleted(Queue queue)
        {
            this.queue = queue;
        }

        public Queue Queue
        {
            get { return queue; }
        }
    }
}