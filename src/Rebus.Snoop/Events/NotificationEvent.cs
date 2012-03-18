namespace Rebus.Snoop.Events
{
    public class NotificationEvent
    {
        readonly string text;

        public NotificationEvent(string message, params  object[] objs)
        {
            text = string.Format(message, objs);
        }

        public string Text
        {
            get { return text; }
        }
    }
}