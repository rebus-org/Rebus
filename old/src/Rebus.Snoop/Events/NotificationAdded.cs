using Rebus.Snoop.ViewModel.Models;

namespace Rebus.Snoop.Events
{
    public class NotificationAdded
    {
        readonly Notification notification;

        public NotificationAdded(Notification notification)
        {
            this.notification = notification;
        }

        public Notification Notification
        {
            get { return notification; }
        }
    }
}