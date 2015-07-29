using Rebus.Snoop.ViewModel.Models;

namespace Rebus.Snoop.Events
{
    public class DeleteQueueRequested
    {
        readonly Queue queue;

        public DeleteQueueRequested(Queue queue)
        {
            this.queue = queue;
        }

        public Queue Queue
        {
            get { return queue; }
        }
    }
}