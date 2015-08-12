using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using GalaSoft.MvvmLight.Messaging;
using Rebus.Snoop.Events;
using Rebus.Snoop.ViewModel.Models;
using System.Linq;

namespace Rebus.Snoop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            Application.Current.DispatcherUnhandledException += HandleUnhandledException;
            Context.Init();
            InitializeComponent();
            Messenger.Default.Register(this, (NotificationAdded n) => HandleNotificationAdded(n));
        }

        void HandleUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            var result = MessageBox.Show(string.Format(@"An exception was caught: {0}

Would you like to continue? (YES: The application might become unstable. NO: The application will quit.)", e.Exception),
                                         "Unhandled exception", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                e.Handled = true;
            }
            else
            {
                Environment.Exit(1);
            }
        }

        void HandleNotificationAdded(NotificationAdded notificationAdded)
        {
            if (NotificationsListBox.Items.Count == 0) return;
            NotificationsListBox.ScrollIntoView(NotificationsListBox.Items[NotificationsListBox.Items.Count-1]);
        }

        void SelectedQueueChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count != 1) return;
            
            var queue = e.AddedItems[0] as Queue;
            if (queue == null) return;
            if (queue.Initialized) return;

            Messenger.Default.Send(new ReloadMessagesRequested(queue));
        }

        readonly List<Message> selectedMessages = new List<Message>();

        void SelectedMessageChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedMessages.AddRange(e.AddedItems.OfType<Message>());
            e.RemovedItems.OfType<Message>().ToList().ForEach(m => selectedMessages.Remove(m));
            Messenger.Default.Send(new MessageSelectionWasMade(selectedMessages));
        }

        void LogLineDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            var listBox = (ListBox) sender;
            var selectedNotification = (Notification) listBox.SelectedItem;

            if (string.IsNullOrEmpty(selectedNotification.FullText)) return;
            
            MessageBox.Show(selectedNotification.FullText);
        }
    }
}
