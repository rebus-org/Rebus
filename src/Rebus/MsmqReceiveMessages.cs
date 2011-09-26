using System.Messaging;

namespace Rebus
{
    class MsmqReceiveMessages : IReceiveMessages
    {
        readonly MessageQueue messageQueue;

        public MsmqReceiveMessages(string path, IProvideMessageTypes provideMessageTypes)
        {
        }
    }
}