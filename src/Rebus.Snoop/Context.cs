using System.Threading.Tasks;

namespace Rebus.Snoop
{
    public class Context
    {
        public static TaskScheduler UiThread
        {
            get { return TaskScheduler.FromCurrentSynchronizationContext(); }
        }
    }
}