namespace Rebus2.Msmq
{
    public class MsmqTransport
    {
        readonly string _inputQueueName;

        public MsmqTransport(string inputQueueName)
        {
            _inputQueueName = inputQueueName;
        }
    }
}