using System;
using System.IO;
using Rebus.Transports.FileSystem;

namespace Rebus.Tests.Contracts.Transports.Factories
{
    public class FileSystemTransportFactory : ITransportFactory
    {
        readonly string baseDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "file_system_transport");

        public Tuple<ISendMessages, IReceiveMessages> Create()
        {
            var sender = new FileSystemMessageQueue(baseDirectory, "test_input_lol_sender");
            var receiver = new FileSystemMessageQueue(baseDirectory, "test_input_lol_receiver");
            
            return new Tuple<ISendMessages, IReceiveMessages>(sender, receiver);
        }

        public IReceiveMessages CreateReceiver(string queueName)
        {
            var queue = new FileSystemMessageQueue(baseDirectory, queueName);

            return queue;
        }

        public void CleanUp()
        {
            if (Directory.Exists(baseDirectory))
            {
                Directory.Delete(baseDirectory, true);
            }
        }
    }
}