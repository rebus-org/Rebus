using Rebus.Snoop.ViewModel.Models;

namespace Rebus.Snoop.Events
{
    public class ReloadQueuesRequested
    {
        readonly Machine machine;

        public ReloadQueuesRequested(Machine machine)
        {
            this.machine = machine;
        }

        public Machine Machine
        {
            get { return machine; }
        }
    }
}