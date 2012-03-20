using System.Windows;
using System.Windows.Controls;
using GalaSoft.MvvmLight.Messaging;
using Rebus.Snoop.Events;
using Rebus.Snoop.ViewModel.Models;

namespace Rebus.Snoop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        void SelectedQueueChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count != 1) return;
            
            var queue = e.AddedItems[0] as Queue;

            if (queue == null) return;

            if (queue.Initialized) return;

            Messenger.Default.Send(new ReloadMessagesRequested(queue));
        }
    }
}
