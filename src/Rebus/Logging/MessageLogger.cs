namespace Rebus.Logging
{
    class MessageLogger
    {
        static ILog log;

        static MessageLogger()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        public void LogSend(string destination, object messageToSend)
        {
            log.Debug("Sending {0} to {1}", messageToSend, destination);
        }

        public void LogReceive(string id, object receivedMessage)
        {
            log.Debug("Dispatching message {0}: {1}", id, receivedMessage.ToString());
        }
    }
}