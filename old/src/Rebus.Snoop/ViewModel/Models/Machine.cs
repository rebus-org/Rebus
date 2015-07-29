using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Rebus.Snoop.Events;

namespace Rebus.Snoop.ViewModel.Models
{
    public class Machine : ViewModel
    {
        readonly ObservableCollection<Queue> queues = new ObservableCollection<Queue>();
#pragma warning disable 649
        string machineName;
        bool success;
#pragma warning restore 649

        public Machine()
        {
            ReloadQueuesCommand = new RelayCommand<Machine>(m => Messenger.Default.Send(new ReloadQueuesRequested(m)));
        }

        public string MachineName
        {
            get { return machineName; }
            set { SetValue(() => MachineName, value); }
        }

        public ObservableCollection<Queue> Queues
        {
            get { return queues; }
        }

        public bool Success
        {
            get { return success; }
            set { SetValue( () => Success, value); }
        }
       
        public void SetQueues(IEnumerable<Queue> newQueues)
        {
            Queues.Clear();

            foreach (var queue in newQueues.Distinct())
            {
                Queues.Add(queue);
            }
        }

        public RelayCommand<Machine> ReloadQueuesCommand { get; set; }
    }
}