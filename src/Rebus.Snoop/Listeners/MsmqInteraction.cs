using System;
using System.ComponentModel;
using System.Linq;
using System.Messaging;
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
            var worker = new BackgroundWorker();
            worker.DoWork += (o, ea) =>
                                 {
                                     var machine = machineAdded.Machine;
                                     try
                                     {
                                         var privateQueues = MessageQueue.GetPrivateQueuesByMachine(machine.MachineName);
                                         var queues = privateQueues.Select(q => new Queue {QueueName = q.QueueName}).ToList();
                                         machine.SetQueues(queues);
                                         ea.Result = new NotificationEvent("{0} queues loaded from {1}.", queues.Count,
                                                                           machine.MachineName);
                                     }
                                     catch(Exception e)
                                     {
                                         ea.Result = new NotificationEvent("Could not load queues from {0}: {1}.", machine.MachineName, e.Message);
                                     }
                                 };
            worker.RunWorkerCompleted += (o, ea) => Messenger.Default.Send((NotificationEvent) ea.Result);
            worker.RunWorkerAsync();
        }
    }
}