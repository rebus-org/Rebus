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
            Messenger.Default.Register(this, (MachineAdded newMachineCreated) => DoLoadQueues(newMachineCreated.Machine));
            Messenger.Default.Register(this, (ReloadQueuesRequested request) => DoLoadQueues(request.Machine));
        }

        static void DoLoadQueues(Machine machine)
        {
            var uiThread = TaskScheduler.FromCurrentSynchronizationContext();

            Task.Factory
                .StartNew(() =>
                              {
                                  var privateQueues = MessageQueue.GetPrivateQueuesByMachine(machine.MachineName);

                                  return privateQueues.Select(q => q.QueueName).ToArray();
                              })
                .ContinueWith(t =>
                                  {
                                      if (!t.IsFaulted)
                                      {
                                          machine.SetQueues(t.Result.Select(name => new Queue {QueueName = name}));

                                          return new NotificationEvent("{0} queues loaded from {1}.",
                                                                       t.Result.Length,
                                                                       machine.MachineName);
                                      }

                                      return new NotificationEvent("Could not load queues from {0}: {1}.",
                                                                   machine.MachineName, t.Exception.Message);
                                  }, uiThread)
                .ContinueWith(t => Messenger.Default.Send(t.Result),
                              uiThread);
        }
    }
}