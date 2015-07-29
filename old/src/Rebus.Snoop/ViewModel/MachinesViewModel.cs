using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Rebus.Shared;
using Rebus.Snoop.Events;
using Rebus.Snoop.ViewModel.Models;
using Queue = Rebus.Snoop.ViewModel.Models.Queue;

namespace Rebus.Snoop.ViewModel
{
    public class MachinesViewModel : ViewModel
    {
        readonly ObservableCollection<Machine> machines = new ObservableCollection<Machine>();
        readonly ObservableCollection<Notification> notifications = new ObservableCollection<Notification>();

#pragma warning disable 649
        bool canMoveMessagesToSourceQueue;
        bool canDeleteMessages;
        bool canDownloadMessages;
        bool canUpdateMessage;
        bool canMoveMessages;
        bool canCopyMessages;
#pragma warning restore 649

        public MachinesViewModel()
        {
            if (IsInDesignMode)
            {
                machines.Add(new Machine
                                 {
                                     MachineName = "another_machine",
                                     Queues =
                                         {
                                             new Queue
                                                 {
                                                     QueueName = "aService.input",
                                                     Messages =
                                                         {
                                                             new Message
                                                                 {
                                                                     Id="74839473829-43278493",
                                                                     Label = "msg1",
                                                                     Bytes = 1235,
                                                                     Time = new DateTime(2012, 03, 19, 12, 30, 45),
                                                                     Headers =
                                                                         {
                                                                             {"rebus-content-type", "text/json"},
                                                                             {"rebus-msg-id", "343982043-439204382048"},
                                                                             {"rebus-return-address","./private$/some_other_queue"},
                                                                             {"rebus-error-message",string.Join(Environment.NewLine, Enumerable.Repeat(new string('*', 250), 20))},
                                                                         },
                                                                     Body =
                                                                         @"{
    ""someProperty"": ""someValue"",
    ""anotherProperty"": {
        ""embedded1"": 1,
        ""embedded2"": 2,
        ""embedded3"": 4,
    }
}"
                                                                 },
                                                             new Message
                                                                 {
                                                                     Id="74839473829-43278673",
                                                                     Label = "msg2",
                                                                     Bytes = 12355,
                                                                     Time = new DateTime(2012, 02, 15, 12, 30, 45),
                                                                     Headers = {{"rebus-content-type", "text/xml"}}
                                                                 },
                                                             new Message
                                                                 {
                                                                     Id="74839473829-43274323",
                                                                     Label = "msg3",
                                                                     Bytes = 123553456,
                                                                     Time = new DateTime(2012, 03, 19, 13, 30, 45),
                                                                     Headers = {{"rebus-content-type", "text/atom"}}
                                                                 }
                                                         }
                                                 },
                                             new Queue {QueueName = "aService.error"},
                                             new Queue {QueueName = "unrelated"},
                                             new Queue {QueueName = "another.unrelated"},
                                             new Queue {QueueName = "Dead-letter queue", CanBeDeleted = false},
                                             new Queue
                                                 {
                                                     QueueName = "yet.another.unrelated",
                                                     Messages =
                                                         {
                                                             new Message
                                                                 {
                                                                     Label = "msg1",
                                                                     Bytes = 12,
                                                                     Time = new DateTime(2012, 03, 19, 12, 30, 45)
                                                                 },
                                                             new Message
                                                                 {
                                                                     Label = "msg2",
                                                                     Bytes = 90,
                                                                     Time = new DateTime(2012, 03, 19, 12, 30, 45)
                                                                 },
                                                             new Message
                                                                 {
                                                                     Label = "msg3",
                                                                     Bytes = 1024,
                                                                     Time = new DateTime(2012, 03, 19, 12, 30, 45)
                                                                 },
                                                             new Message {Label = "msg4", Bytes = 2048},
                                                             new Message {Label = "msg5", Bytes = 10249090},
                                                             new Message {Label = "msg6", Bytes = 3424234},
                                                             new Message {Label = "msg7", Bytes = 15325323},
                                                             new Message {Label = "msg8", Bytes = 15352},
                                                             new Message {Label = "msg9", Bytes = 12},
                                                         }
                                                 },
                                         }
                                 });
                machines.Add(new Machine
                                 {
                                     MachineName = "some_machine",
                                     Queues =
                                         {
                                             new Queue
                                                 {
                                                     QueueName = "someService.input",
                                                     Messages =
                                                         {
                                                             new Message {Label = "msg1", Bytes = 123556},
                                                             new Message {Label = "msg2", Bytes = 48374977},
                                                             new Message {Label = "msg3", Bytes = 345}
                                                         }
                                                 },
                                             new Queue
                                                 {
                                                     QueueName = "someService.error",
                                                     Messages = {new Message {Label = "some.error.msg"},}
                                                 },
                                             new Queue {QueueName = "anotherService.input"},
                                             new Queue {QueueName = "anotherService.error"},
                                         }
                                 });

                machines.Add(new Machine { MachineName = "yet_another_machine" });

                notifications.Add(new Notification("4 queues loaded from some_machine"));
                notifications.Add(new Notification("5 queues loaded from another_machine"));
            }
            else
            {
                AddNewMachine("localhost");
            }

            CreateCommands();
            RegisterListeners();
        }

        public bool CanMoveMessagesToSourceQueue
        {
            get { return canMoveMessagesToSourceQueue; }
            set { SetValue(() => CanMoveMessagesToSourceQueue, value); }
        }

        public bool CanMoveMessages
        {
            get { return canMoveMessages; }
            set { SetValue(() => CanMoveMessages, value); }
        }

        public bool CanCopyMessages
        {
            get { return canCopyMessages; }
            set { SetValue(() => CanCopyMessages, value); }
        }

        public bool CanDeleteMessages
        {
            get { return canDeleteMessages; }
            set { SetValue(() => CanDeleteMessages, value); }
        }

        public bool CanUpdateMessage
        {
            get { return canUpdateMessage; }
            set { SetValue(() => CanUpdateMessage, value); }
        }

        public bool CanDownloadMessages
        {
            get { return canDownloadMessages; }
            set { SetValue(() => CanDownloadMessages, value); }
        }

        public ObservableCollection<Machine> Machines
        {
            get { return machines; }
        }

        public RelayCommand<string> AddMachineCommand { get; set; }

        public RelayCommand<Machine> RemoveMachineCommand { get; set; }

        public RelayCommand<IEnumerable> ReturnToSourceQueuesCommand { get; set; }
        
        public RelayCommand<IEnumerable> MoveToQueueCommand { get; set; }
        
        public RelayCommand<IEnumerable> CopyToQueueCommand { get; set; }

        public RelayCommand<IEnumerable> DeleteMessagesCommand { get; set; }
        
        public RelayCommand<IEnumerable> DownloadMessagesCommand { get; set; }

        public RelayCommand<IEnumerable> UpdateMessageCommand { get; set; }

        public ObservableCollection<Notification> Notifications
        {
            get { return notifications; }
        }

        void RegisterListeners()
        {
            Messenger.Default.Register(this, (NotificationEvent n) => AddNotification(n));
            Messenger.Default.Register(this, (MessageSelectionWasMade n) => HandleMessageSelectionWasMade(n));
            Messenger.Default.Register(this, (MessageMoved m) => HandleMessageMoved(m));
            Messenger.Default.Register(this, (MessageDeleted m) => HandleMessageDeleted(m));
            Messenger.Default.Register(this, (QueuePurged m) => HandleQueuePurged(m));
            Messenger.Default.Register(this, (QueueDeleted m) => HandleQueueDeleted(m));
        }

        void HandleQueueDeleted(QueueDeleted queueDeleted)
        {
            var queue = queueDeleted.Queue;

            var machineWhereQueueWasDeleted = Machines.FirstOrDefault(m => m.Queues.Contains(queue));
            if (machineWhereQueueWasDeleted == null) return;

            Messenger.Default.Send(new ReloadQueuesRequested(machineWhereQueueWasDeleted));
        }

        void HandleQueuePurged(QueuePurged queuePurged)
        {
            var queue = queuePurged.Queue;
            
            // reload queue in question
            Messenger.Default.Send(new ReloadMessagesRequested(queue));

            // if possible, reload dead-letter queue as well
            var machineWhereQueueWasPurged = Machines.FirstOrDefault(m => m.Queues.Contains(queue));
            if (machineWhereQueueWasPurged == null) return;

            var deadLetterQueue = machineWhereQueueWasPurged.Queues
                                                            .FirstOrDefault(q => q.QueuePath.ToLowerInvariant()
                                                                                  .Contains("deadxact"));
            // if the purged queue was the dead-letter queue, don't bother :)
            if (queue == deadLetterQueue)
            {
                return;
            }

            Messenger.Default.Send(new ReloadMessagesRequested(deadLetterQueue));
        }


        void HandleMessageDeleted(MessageDeleted messageDeleted)
        {
            Task.Factory
                .StartNew(() =>
                    {
                        var allQueues = Machines.SelectMany(m => m.Queues).ToArray();

                        // update involved queues if they have been loaded
                        var message = messageDeleted.MessageThatWasDeleted;
                        var sourceQueue =
                            allQueues.FirstOrDefault(
                                q => q.QueuePath.Equals(message.QueuePath, StringComparison.InvariantCultureIgnoreCase));

                        return new
                            {
                                SourceQueue = sourceQueue,
                                Message = message,
                            };
                    })
                .ContinueWith(a =>
                    {
                        var result = a.Result;

                        result.SourceQueue.Remove(result.Message);
                    }, Context.UiThread);
        }

        void HandleMessageMoved(MessageMoved messageMoved)
        {
            //BAH! just ignore for now!
            Task.Factory
                .StartNew(() =>
                              {
                                  // attempt to update view models to avoid the need to refresh everything
                                  var theMessage = messageMoved.MessageThatWasMoved;
                                  var sourceQueuePath = messageMoved.SourceQueuePath;
                                  var destinationQueuePath = messageMoved.DestinationQueuePath;

                                  var allQueues = Machines.SelectMany(m => m.Queues).ToArray();

                                  // update involved queues if they have been loaded
                                  var sourceQueue = allQueues.FirstOrDefault(q => q.QueuePath.Equals(sourceQueuePath, StringComparison.InvariantCultureIgnoreCase));
                                  var destinationQueue = allQueues.FirstOrDefault(q => q.QueuePath.Equals(destinationQueuePath, StringComparison.InvariantCultureIgnoreCase));

                                  var result = new
                                                   {
                                                       SourceQueue = sourceQueue,
                                                       SourceQueuePath = sourceQueuePath,
                                                       DestinationQueue = destinationQueue,
                                                       DestinationQueuePath = destinationQueuePath,
                                                       TheMessage = theMessage,
                                                       CopyWasLeftInSourceQueue = messageMoved.CopyWasLeftInSourceQueue,
                                                   };

                                  return result;
                              })
                .ContinueWith(a =>
                                  {
                                      var result = a.Result;

                                      if (result.SourceQueue != null)
                                      {
                                          result.SourceQueue.Remove(result.TheMessage);
                                          
                                          if (result.CopyWasLeftInSourceQueue)
                                          {
                                              result.SourceQueue.Add(result.TheMessage.Clone());
                                          }
                                      }

                                      if (result.DestinationQueue != null)
                                      {
                                          result.DestinationQueue.Add(result.TheMessage);
                                      }
                                  },
                              Context.UiThread);
        }

        void HandleMessageSelectionWasMade(MessageSelectionWasMade messageSelectionWasMade)
        {
            var oneOrMoreSelectedMessagesHasSourceQueueHeader = messageSelectionWasMade.SelectedMessages.Any(m => m.Headers.ContainsKey(Headers.SourceQueue));
            var oneOrMoreMessagesSelected = messageSelectionWasMade.SelectedMessages.Any();
            var exactlyOneMessageIsSelected = messageSelectionWasMade.SelectedMessages.Count() == 1;

            CanMoveMessagesToSourceQueue = oneOrMoreSelectedMessagesHasSourceQueueHeader;
            CanMoveMessages = oneOrMoreMessagesSelected;
            CanCopyMessages = oneOrMoreMessagesSelected;
            CanDeleteMessages = oneOrMoreMessagesSelected;
            CanDownloadMessages = oneOrMoreMessagesSelected;
            CanUpdateMessage = exactlyOneMessageIsSelected;
        }

        void AddNotification(NotificationEvent n)
        {
            var notification = new Notification(n.Text, n.Details);
            notifications.Add(notification);
            Messenger.Default.Send(new NotificationAdded(notification));
        }

        void CreateCommands()
        {
            AddMachineCommand = new RelayCommand<string>(AddNewMachine);
            RemoveMachineCommand = new RelayCommand<Machine>(RemoveMachine);
            ReturnToSourceQueuesCommand = new RelayCommand<IEnumerable>(ReturnToSourceQueues);
            MoveToQueueCommand = new RelayCommand<IEnumerable>(MoveToQueue);
            CopyToQueueCommand = new RelayCommand<IEnumerable>(CopyToQueue);
            DeleteMessagesCommand = new RelayCommand<IEnumerable>(DeleteMessages);
            DownloadMessagesCommand = new RelayCommand<IEnumerable>(DownloadMessages);
            UpdateMessageCommand = new RelayCommand<IEnumerable>(UpdateMessage);
        }

        void UpdateMessage(IEnumerable list)
        {
            var message = list.OfType<Message>().Single();

            var queue = Machines.SelectMany(m => m.Queues)
                                .First(q => q.Messages.Contains(message));

            Messenger.Default.Send(new UpdateMessageRequested(message, queue));
        }

        void DownloadMessages(IEnumerable messages)
        {
            var messagesToMove = messages.OfType<Message>().ToList();

            Messenger.Default.Send(new DownloadMessagesRequested(messagesToMove));
        }

        void DeleteMessages(IEnumerable messages)
        {
            var messagesToMove = messages.OfType<Message>().ToList();

            Messenger.Default.Send(new DeleteMessagesRequested(messagesToMove));
        }

        void ReturnToSourceQueues(IEnumerable messages)
        {
            var messagesToMove = messages.OfType<Message>().ToList();

            Messenger.Default.Send(new MoveMessagesToSourceQueueRequested(messagesToMove));
        }

        void MoveToQueue(IEnumerable messages)
        {
            var messagesToMove = messages.OfType<Message>().ToList();

            Messenger.Default.Send(new MoveMessagesToQueueRequested(messagesToMove));
        }

        void CopyToQueue(IEnumerable messages)
        {
            var messagesToMove = messages.OfType<Message>().ToList();

            Messenger.Default.Send(new CopyMessagesToQueueRequested(messagesToMove));
        }

        void AddNewMachine(string newMachineName)
        {
            if (string.IsNullOrEmpty(newMachineName)) return;
            if (Machines.Any(m => m.MachineName == newMachineName)) return;

            var newMachine = new Machine { MachineName = newMachineName };
            Machines.Add(newMachine);

            Messenger.Default.Send(new MachineAdded(newMachine));
        }

        void RemoveMachine(Machine machine)
        {
            Machines.Remove(machine);
        }
    }
}