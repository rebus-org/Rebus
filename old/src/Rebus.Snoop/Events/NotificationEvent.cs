namespace Rebus.Snoop.Events
{
    public class NotificationEvent
    {
        NotificationEvent(string message, params  object[] objs)
        {
            Text = string.Format(message, objs);
        }

        public string Text { get; private set; }
        public string Details { get; private set; }

        public static NotificationEvent Success(string message, params object[] objs)
        {
            return new NotificationEvent(message, objs);
        }

        public static NotificationEvent Fail(string details, string message, params object[]objs)
        {
            return new NotificationEvent(message, objs) {Details = details};
        }
    }
}