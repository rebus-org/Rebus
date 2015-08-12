using System.Threading.Tasks;

namespace Rebus.Snoop
{
    public class Context
    {
        static TaskScheduler uiThread;

        public static void Init()
        {
            uiThread = TaskScheduler.FromCurrentSynchronizationContext();
        }

        public static TaskScheduler UiThread
        {
            get { return uiThread; }
        }
    }
}