using System;
using Rebus.Tests.Performance.StressMongo.Crm;

namespace Rebus.Tests.Performance.StressMongo.Legal
{
    internal class CheckSomeLegalStuffSaga
        : Saga<CheckSomeLegalStuffSagaData>,
          IAmInitiatedBy<CustomerCreated>,
          IHandleMessages<LegalCheckComplete>
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
            Incoming<LegalCheckComplete>(m => m.CustomerId).CorrelatesWith(d => d.CustomerId);
        }

        public void Handle(CustomerCreated message)
        {
            if (Data.CustomerId == Guid.Empty)
            {
                var customerId = message.CustomerId;

                flowLog.Log(customerId, "Commencing legal check of {0}", message.Name);

                Data.CustomerId = customerId;

                bus.Defer(8.Seconds(), new LegalCheckComplete {CustomerId = customerId});
            }
        }

        public void Handle(LegalCheckComplete message)
        {
            flowLog.Log(message.CustomerId, "Legal check completed!");

            MarkAsComplete();
        }
    }

    class LegalCheckComplete
    {
        public Guid CustomerId { get; set; }
    }
}