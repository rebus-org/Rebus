using Rebus.Snoop.ViewModel.Models;

namespace Rebus.Snoop.Events
{
    public class MachineAdded
    {
        readonly Machine machine;

        public MachineAdded(Machine machine)
        {
            this.machine = machine;
        }

        public Machine Machine
        {
            get { return machine; }
        }
    }
}