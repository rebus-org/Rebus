using System.Collections.Generic;
using System.Linq;
using System.Messaging;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Messaging;
using Rebus.Snoop.Events;
using Rebus.Snoop.ViewModel.Models;
using Message = Rebus.Snoop.ViewModel.Models.Message;
using Rebus.Snoop.Msmq;

namespace Rebus.Snoop.Listeners
{
    public class MsmqInteraction
    {
        public MsmqInteraction()
        {
            Messenger.Default.Register(this, (MachineAdded newMachineCreated) => LoadQueues(newMachineCreated.Machine));
            Messenger.Default.Register(this, (ReloadQueuesRequested request) => LoadQueues(request.Machine));
            Messenger.Default.Register(this, (ReloadMessagesRequested request) => LoadMessages(request.Queue));
        }

        void LoadMessages(Queue queue)
        {
            Task.Factory
                .StartNew(() =>
                              {
                                  var messageQueue = new MessageQueue(queue.QueuePath);
                                  var list = new List<Message>();

                                  using (var enumerator = messageQueue.GetMessageEnumerator2())
                                  {
                                      while (enumerator.MoveNext())
                                      {
                                          var message = enumerator.Current;
                                          list.Add(new Message {Label = message.Label});
                                      }
                                  }

                                  return list;
                              })
                .ContinueWith(t =>
                                  {
                                      if (!t.IsFaulted)
                                      {
                                          queue.SetMessages(t.Result);
                                          return new NotificationEvent("{0} messages loaded from {1}", t.Result.Count,
                                                                       queue.QueueName);
                                      }
                                      
                                      return new NotificationEvent("Could not load messages from {0}: {1}",
                                                                   queue.QueueName,
                                                                   t.Exception);
                                  }, UiThread)
                .ContinueWith(t => Messenger.Default.Send(t.Result), UiThread);
        }

        void LoadQueues(Machine machine)
        {
            Task.Factory
                .StartNew(() =>
                              {
                                  var privateQueues = MessageQueue.GetPrivateQueuesByMachine(machine.MachineName);

                                  return privateQueues;
                              })
                .ContinueWith(t =>
                                  {
                                      if (!t.IsFaulted)
                                      {
                                          var queues = t.Result
                                              .Select(queue => new Queue(queue));

                                          machine.SetQueues(queues);

                                          return new NotificationEvent("{0} queues loaded from {1}",
                                                                       t.Result.Length,
                                                                       machine.MachineName);
                                      }

                                      return new NotificationEvent("Could not load queues from {0}: {1}",
                                                                   machine.MachineName, t.Exception.Message);
                                  }, UiThread)
                .ContinueWith(t => Messenger.Default.Send(t.Result), UiThread);
        }

        static TaskScheduler UiThread
        {
            get { return TaskScheduler.FromCurrentSynchronizationContext(); }
        }
    }
}