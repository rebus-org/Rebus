namespace Rebus
{
    public class FakeMessageContext
    {
        public static void Establish(IMessageContext messageContext)
        {
            MessageContext.current = messageContext;
        }

        public static void Reset()
        {
            MessageContext.current = null;
        }
    }
}