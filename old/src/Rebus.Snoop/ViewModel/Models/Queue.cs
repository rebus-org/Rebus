using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Messaging;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Rebus.Snoop.Events;
using Rebus.Snoop.Msmq;

namespace Rebus.Snoop.ViewModel.Models
{
    [DebuggerDisplay("{QueuePath}")]
    public class Queue : ViewModel
    {
        readonly ObservableCollection<Message> messages = new ObservableCollection<Message>();
#pragma warning disable 649
        int messageCount;
        string queueName;
        string queuePath;
        bool initialized;
        bool canBeDeleted;
#pragma warning restore 649

        public Queue()
        {
            ReloadMessagesCommand = new RelayCommand<Queue>(q => Messenger.Default.Send(new ReloadMessagesRequested(q)));
            PurgeMessagesCommand = new RelayCommand<Queue>(q => Messenger.Default.Send(new PurgeMessagesRequested(q)));
            DeleteQueueCommand = new RelayCommand<Queue>(q => Messenger.Default.Send(new DeleteQueueRequested(q)));
            CanBeDeleted = true;
        }

        public bool CanBeDeleted
        {
            get { return canBeDeleted; }
            set { SetValue(() => CanBeDeleted, value); }
        }

        public bool Initialized
        {
            get { return initialized; }
            private set { SetValue(() => Initialized, value); }
        }

        public Queue(MessageQueue queue)
            : this()
        {
            QueuePath = queue.Path;
            try
            {
                QueueName = queue.QueueName;
            }
            catch (Exception e)
            {
                if (queue.Path.ToLowerInvariant()
                         .Contains("deadxact"))
                {
                    QueueName = "Dead-letter queue (TX)";
                    CanBeDeleted = false;
                }
                else
                {
                    Messenger.Default.Send(NotificationEvent.Fail(e.ToString(),
                                                                  "An error occurred while getting queue name for {0}: {1}",
                                                                  queue.Path, e.Message));
                    QueueName = QueuePath;
                }
            }

            try
            {
                MessageCount = (int)queue.GetCount();
            }
            catch (Exception e)
            {
                Messenger.Default.Send(NotificationEvent.Fail(e.ToString(),
                                                              "An error occurred while retrieving message count from {0}: {1}",
                                                              QueueName, e.Message));
                MessageCount = -1;
            }
        }

        public string QueueName
        {
            get { return queueName; }
            set { SetValue(() => QueueName, value); }
        }

        public string QueuePath
        {
            get { return queuePath; }
            set { SetValue(() => QueuePath, value); }
        }

        public int MessageCount
        {
            get { return messageCount; }
            set { SetValue(() => MessageCount, value); }
        }

        public ObservableCollection<Message> Messages
        {
            get { return messages; }
        }

        public RelayCommand<Queue> ReloadMessagesCommand { get; set; }
        
        public RelayCommand<Queue> PurgeMessagesCommand { get; set; }
        
        public RelayCommand<Queue> DeleteQueueCommand { get; set; }

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

            // in case the queue hasn't been initialized, we need to just increment this number
            MessageCount++;

            message.QueuePath = QueuePath;
        }
    }
}