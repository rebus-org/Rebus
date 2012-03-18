using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Rebus.Snoop.ViewModel.Models
{
    public class Machine : ViewModel
    {
        readonly ObservableCollection<Queue> queues = new ObservableCollection<Queue>();
        string machineName;
        bool success;

        public string MachineName
        {
            get { return machineName; }
            set { SetValue("MachineName", value); }
        }

        public ObservableCollection<Queue> Queues
        {
            get { return queues; }
        }

        public bool Success
        {
            get { return success; }
            set { SetValue("Success", value); }
        }
       
        public void SetQueues(IEnumerable<Queue> newQueues)
        {
            Queues.Clear();

            foreach (var queue in newQueues.Distinct())
            {
                Queues.Add(queue);
            }
        }
    }
}