using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Messaging;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Rebus.Snoop.Events;
using Rebus.Snoop.Msmq;

namespace Rebus.Snoop.ViewModel.Models
{
    public class Queue : ViewModel
    {
        readonly ObservableCollection<Message> messages = new ObservableCollection<Message>();
        uint messageCount;
        string queueName;
        string queuePath;

        public Queue()
        {
            ReloadMessagesCommand = new RelayCommand<Queue>(q => Messenger.Default.Send(new ReloadMessagesRequested(q)));
        }

        public Queue(MessageQueue queue)
            : this()
        {
            QueueName = queue.QueueName;
            QueuePath = queue.Path;
            MessageCount = queue.GetCount();
        }

        public string QueueName
        {
            get { return queueName; }
            set { SetValue("QueueName", value); }
        }

        public string QueuePath
        {
            get { return queuePath; }
            set { SetValue("QueuePath", value); }
        }

        public uint MessageCount
        {
            get { return messageCount; }
            set { SetValue("MessageCount", value); }
        }

        public ObservableCollection<Message> Messages
        {
            get { return messages; }
        }

        public RelayCommand<Queue> ReloadMessagesCommand { get; set; }

        public void SetMessages(List<Message> result)
        {
            messages.Clear();
            foreach (Message message in result)
            {
                messages.Add(message);
            }
        }
    }
}