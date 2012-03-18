using System.Collections.ObjectModel;

namespace Rebus.Snoop.ViewModel.Models
{
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
}