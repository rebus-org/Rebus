using Rebus.Snoop.ViewModel.Models;

namespace Rebus.Snoop.Events
{
    public class QueuePurged
    {
        readonly Queue queue;

        public QueuePurged(Queue queue)
        {
            this.queue = queue;
        }

        public Queue Queue
        {
            get { return queue; }
        }
    }
}