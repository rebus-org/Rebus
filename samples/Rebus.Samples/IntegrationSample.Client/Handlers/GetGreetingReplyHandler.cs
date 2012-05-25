using System;
using IntegrationSample.IntegrationService.Messages;
using Rebus;

namespace IntegrationSample.Client.Handlers
{
    public class GetGreetingReplyHandler : IHandleMessages<GetGreetingReply>
    {
        public void Handle(GetGreetingReply message)
        {
            Console.WriteLine("Got greeting reply: {0}", message.TheGreeting);
        }
    }
}