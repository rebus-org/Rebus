using System.Linq;
using System.Messaging;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Messaging;
using Rebus.Snoop.Events;
using Rebus.Snoop.ViewModel.Models;

namespace Rebus.Snoop.Listeners
{
    public class MsmqInteraction
    {
        public MsmqInteraction()
        {
            Messenger.Default.Register(this, (MachineAdded newMachineCreated) => LoadQueues(newMachineCreated));
        }

        void LoadQueues(MachineAdded machineAdded)
        {
            Task.Factory
                .StartNew(() =>
                              {
                                  var machine = machineAdded.Machine;
                                  var privateQueues = MessageQueue.GetPrivateQueuesByMachine(machine.MachineName);
                                  var queues = privateQueues.Select(q => new Queue { QueueName = q.QueueName });
                                  machine.SetQueues(queues);
                              });
        }
    }
}