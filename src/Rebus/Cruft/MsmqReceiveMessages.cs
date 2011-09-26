using System.Messaging;

namespace Rebus.Cruft
{
    class MsmqReceiveMessages : IReceiveMessages
    {
        readonly MessageQueue messageQueue;

        public MsmqReceiveMessages(string path, IProvideMessageTypes provideMessageTypes)
        {
        }
    }
}