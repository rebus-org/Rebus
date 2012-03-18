using System.Collections.ObjectModel;
using System.Linq;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Rebus.Snoop.Events;
using Rebus.Snoop.ViewModel.Models;

namespace Rebus.Snoop.ViewModel
{
    public class MachinesViewModel : ViewModel
    {
        readonly ObservableCollection<Machine> machines = new ObservableCollection<Machine>();

        public MachinesViewModel()
        {
            //if (IsInDesignMode)
            {
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
                                                             new Message {MessageHeader = "msg1", Bytes = 123556},
                                                             new Message {MessageHeader = "msg2", Bytes = 48374977},
                                                             new Message {MessageHeader = "msg3", Bytes = 345}
                                                         }
                                                 },
                                             new Queue
                                                 {
                                                     QueueName = "someService.error",
                                                     Messages = {new Message {MessageHeader = "some.error.msg"},}
                                                 },
                                             new Queue {QueueName = "anotherService.input"},
                                             new Queue {QueueName = "anotherService.error"},
                                         }
                                 });

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
                                                             new Message {MessageHeader = "msg1", Bytes = 1235},
                                                             new Message {MessageHeader = "msg2", Bytes = 12355},
                                                             new Message {MessageHeader = "msg3", Bytes = 123553456}
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
                                                             new Message {MessageHeader = "msg1", Bytes = 12},
                                                             new Message {MessageHeader = "msg2", Bytes = 90},
                                                             new Message {MessageHeader = "msg3", Bytes = 1024},
                                                             new Message {MessageHeader = "msg4", Bytes = 2048},
                                                             new Message {MessageHeader = "msg5", Bytes = 10249090},
                                                             new Message {MessageHeader = "msg6", Bytes = 3424234},
                                                             new Message {MessageHeader = "msg7", Bytes = 15325323},
                                                             new Message {MessageHeader = "msg8", Bytes = 15352},
                                                             new Message {MessageHeader = "msg9", Bytes = 12},
                                                         }
                                                 },
                                         }
                                 });
                machines.Add(new Machine {MachineName = "yet_another_machine"});
            }

            CreateCommands();
            RegisterListeners();
        }

        void RegisterListeners()
        {
        }

        public ObservableCollection<Machine> Machines
        {
            get { return machines; }
        }

        public RelayCommand<string> AddMachineCommand { get; set; }

        public RelayCommand<Machine> RemoveMachineCommand { get; set; }

        void CreateCommands()
        {
            AddMachineCommand = new RelayCommand<string>(AddNewMachine);
            RemoveMachineCommand = new RelayCommand<Machine>(RemoveMachine);
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