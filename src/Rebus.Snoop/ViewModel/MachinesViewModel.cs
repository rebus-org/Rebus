using System.Collections.ObjectModel;

namespace Rebus.Snoop.Machines
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
                                     MachineName = "\\some_machine",
                                     Queues =
                                         {
                                             new Queue {QueueName = "someService.input", Messages={new Message{MessageHeader="msg1"}, new Message{MessageHeader="msg2"}, new Message{MessageHeader="msg3"}}},
                                             new Queue {QueueName = "someService.error", Messages={new Message{MessageHeader="some.error.msg"},}},
                                             new Queue {QueueName = "anotherService.input"},
                                             new Queue {QueueName = "anotherService.error"},
                                         }
                                 });
                machines.Add(new Machine
                                 {
                                     MachineName = "\\another_machine",
                                     Queues =
                                         {
                                             new Queue {QueueName = "aService.input", Messages={new Message{MessageHeader="msg1"}, new Message{MessageHeader="msg2"}, new Message{MessageHeader="msg3"}}},
                                             new Queue {QueueName = "aService.error"},
                                             new Queue {QueueName = "unrelated"},
                                             new Queue {QueueName = "another.unrelated"},
                                             new Queue {QueueName = "yet.another.unrelated",Messages=
                                                                                                {
                                                                                                    new Message{MessageHeader="msg1", Bytes=12}, 
                                                                                                    new Message{MessageHeader="msg2", Bytes=90}, 
                                                                                                    new Message{MessageHeader="msg3", Bytes=1024},
                                                                                                    new Message{MessageHeader="msg4", Bytes=2048}, 
                                                                                                    new Message{MessageHeader="msg5", Bytes=10249090}, 
                                                                                                    new Message{MessageHeader="msg6", Bytes=3424234},
                                                                                                    new Message{MessageHeader="msg7", Bytes=15325323}, 
                                                                                                    new Message{MessageHeader="msg8", Bytes=15352}, 
                                                                                                    new Message{MessageHeader="msg9", Bytes=12},
                                                                                                }},
                                         }
                                 });
                machines.Add(new Machine {MachineName = "\\yet_another_machine"});
            }
        }

        public ObservableCollection<Machine> Machines
        {
            get { return machines; }
        }
    }

    public class Machine : ViewModel
    {
        readonly ObservableCollection<Queue> queues = new ObservableCollection<Queue>();
        string machineName;

        public string MachineName
        {
            get { return machineName; }
            set { SetValue("MachineName", value); }
        }

        public ObservableCollection<Queue> Queues
        {
            get { return queues; }
        }
    }

    public class Queue : ViewModel
    {
        readonly ObservableCollection<Message> messages = new ObservableCollection<Message>();
        string queueName;

        public string QueueName
        {
            get { return queueName; }
            set { SetValue("QueueName", value); }
        }

        public ObservableCollection<Message> Messages
        {
            get { return messages; }
        }
    }

    public class Message : ViewModel
    {
        string messageHeader;

        public string MessageHeader
        {
            get { return messageHeader; }
            set { SetValue("MessageHeader", value); }
        }

        int bytes;

        public int Bytes
        {
            get { return bytes; }
            set { SetValue("Bytes", value); }
        }

        
    }
}