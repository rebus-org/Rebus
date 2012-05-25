using IntegrationSample.IntegrationService.Messages;
using IntegrationSample.IntegrationService.SomethingExternal;
using Rebus;

namespace IntegrationSample.IntegrationService.Handlers
{
    public class GetGreetingRequestHandler : IHandleMessages<GetGreetingRequest>
    {
        readonly IBus bus;

        public GetGreetingRequestHandler(IBus bus)
        {
            this.bus = bus;
        }

        public void Handle(GetGreetingRequest message)
        {
            using (var client = new Service1Client())
            {
                bus.Reply(new GetGreetingReply {TheGreeting = client.GetGreeting()});
            }
        }
    }
}