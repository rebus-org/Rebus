using Rebus.Snoop.ViewModel.Models;

namespace Rebus.Snoop.Events
{
    public class ReloadMessagesRequested
    {
        readonly Queue queue;

        public ReloadMessagesRequested(Queue queue)
        {
            this.queue = queue;
        }

        public Queue Queue
        {
            get { return queue; }
        }
    }
}