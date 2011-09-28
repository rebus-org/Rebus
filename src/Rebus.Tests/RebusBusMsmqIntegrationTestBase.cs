namespace Rebus.Tests
{
    /// <summary>
    /// Test base class with helpers for running integration tests with
    /// <see cref="RebusBus"/> and <see cref="MsmqMessageQueue"/>.
    /// </summary>
    public class RebusBusMsmqIntegrationTestBase
    {
        protected static RebusBus CreateBus(string inputQueueName, IHandlerFactory handlerFactory)
        {
            var testMessageTypeProvider = new TestMessageTypeProvider();
            var messageQueue = new MsmqMessageQueue(inputQueueName, testMessageTypeProvider);

            return new RebusBus(handlerFactory, messageQueue, messageQueue, new InMemorySubscriptionStorage());
        }

        protected string PrivateQueueNamed(string queueName)
        {
            return string.Format(@".\private$\{0}", queueName);
        }
    }
}