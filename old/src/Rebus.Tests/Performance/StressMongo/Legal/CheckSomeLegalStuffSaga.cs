using System;
using Rebus.Tests.Performance.StressMongo.Crm.Messages;
using Rebus.Tests.Performance.StressMongo.Legal.Messages;

namespace Rebus.Tests.Performance.StressMongo.Legal
{
    class CheckSomeLegalStuffSaga
        : Saga<CheckSomeLegalStuffSagaData>,
          IAmInitiatedBy<CustomerCreated>,
          IHandleMessages<SimulatedLegalCheckComplete>
    {
        readonly IBus bus;
        readonly IFlowLog flowLog;

        public CheckSomeLegalStuffSaga(IBus bus, IFlowLog flowLog)
        {
            this.bus = bus;
            this.flowLog = flowLog;
        }

        public override void ConfigureHowToFindSaga()
        {
            Incoming<CustomerCreated>(m => m.CustomerId).CorrelatesWith(d => d.CustomerId);
            Incoming<SimulatedLegalCheckComplete>(m => m.CustomerId).CorrelatesWith(d => d.CustomerId);
        }

        public void Handle(CustomerCreated message)
        {
            // we're idempotent!
            if (Data.CustomerId != Guid.Empty) return;

            var customerId = message.CustomerId;

            flowLog.LogFlow(customerId, "Commencing legal check of {0}", message.Name);

            Data.CustomerId = customerId;

            bus.Defer(8.Seconds(), new SimulatedLegalCheckComplete {CustomerId = customerId});
        }

        public void Handle(SimulatedLegalCheckComplete message)
        {
            flowLog.LogFlow(message.CustomerId, "Legal check completed!");

            bus.Publish(new CustomerLegallyApproved {CustomerId = message.CustomerId});

            MarkAsComplete();
        }
    }
}