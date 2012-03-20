using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
        readonly ObservableCollection<string> notifications = new ObservableCollection<string>();

        bool canMoveMessagesToSourceQueue;

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
                                                                             {
                                                                                 "rebus-return-address",
                                                                                 "./private$/some_other_queue"
                                                                                 }
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

                machines.Add(new Machine {MachineName = "yet_another_machine"});

                notifications.Add("4 queues loaded from some_machine");
                notifications.Add("5 queues loaded from another_machine");
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
            set { SetValue("CanMoveMessagesToSourceQueue", value); }
        }

        public ObservableCollection<Machine> Machines
        {
            get { return machines; }
        }

        public RelayCommand<string> AddMachineCommand { get; set; }

        public RelayCommand<Machine> RemoveMachineCommand { get; set; }

        public RelayCommand<IEnumerable> ReturnToSourceQueuesCommand { get; set; }

        public ObservableCollection<string> Notifications
        {
            get { return notifications; }
        }

        void RegisterListeners()
        {
            Messenger.Default.Register(this, (NotificationEvent n) => AddNotification(n));
            Messenger.Default.Register(this, (MessageSelectionWasMade n) => HandleMessageSelectionWasMade(n));
        }

        void HandleMessageSelectionWasMade(MessageSelectionWasMade messageSelectionWasMade)
        {
            CanMoveMessagesToSourceQueue =
                messageSelectionWasMade.SelectedMessages.Any(m => m.Headers.ContainsKey(Headers.SourceQueue));
        }

        void AddNotification(NotificationEvent n)
        {
            notifications.Add(n.Text);
        }

        void CreateCommands()
        {
            AddMachineCommand = new RelayCommand<string>(AddNewMachine);
            RemoveMachineCommand = new RelayCommand<Machine>(RemoveMachine);
            ReturnToSourceQueuesCommand = new RelayCommand<IEnumerable>(ReturnToSourceQueues);
        }

        void ReturnToSourceQueues(IEnumerable messages)
        {
            List<Message> messagesToMove = messages.OfType<Message>().ToList();

            Messenger.Default.Send(new MoveMessagesToSourceQueueRequested(messagesToMove));
        }

        void AddNewMachine(string newMachineName)
        {
            if (string.IsNullOrEmpty(newMachineName)) return;
            if (Machines.Any(m => m.MachineName == newMachineName)) return;

            var newMachine = new Machine {MachineName = newMachineName};
            Machines.Add(newMachine);

            Messenger.Default.Send(new MachineAdded(newMachine));
        }

        void RemoveMachine(Machine machine)
        {
            Machines.Remove(machine);
        }
    }
}