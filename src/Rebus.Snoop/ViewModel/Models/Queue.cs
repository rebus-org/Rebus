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
        int messageCount;
        string queueName;
        string queuePath;
        bool initialized;

        public Queue()
        {
            ReloadMessagesCommand = new RelayCommand<Queue>(q => Messenger.Default.Send(new ReloadMessagesRequested(q)));
        }

        public bool Initialized
        {
            get { return initialized; }
            private set { SetValue("Initialized", value); }
        }

        public Queue(MessageQueue queue)
            : this()
        {
            QueueName = queue.QueueName;
            QueuePath = queue.Path;
            MessageCount = (int)queue.GetCount();
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

        public int MessageCount
        {
            get { return messageCount; }
            set { SetValue("MessageCount", value); }
        }

        public ObservableCollection<Message> Messages
        {
            get { return messages; }
        }

        public RelayCommand<Queue> ReloadMessagesCommand { get; set; }

        public void SetMessages(List<Message> messages)
        {
            Messages.Clear();
            foreach (var message in messages)
            {
                Messages.Add(message);
            }
            MessageCount = messages.Count;
            Initialized = true;
        }

        public void Remove(Message message)
        {
            Messages.Remove(message);
            MessageCount = messages.Count;
        }

        public void Add(Message message)
        {
            Messages.Add(message);
            MessageCount = messages.Count;
            message.QueuePath = QueuePath;
        }
    }
}