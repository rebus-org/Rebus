using GalaSoft.MvvmLight;
using Rebus.Snoop.Listeners;

namespace Rebus.Snoop.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        MsmqInteraction msmqInteraction;

        public MainViewModel()
        {
            msmqInteraction = new MsmqInteraction();
        }
    }
}