using System;
using Rebus.Tests.Performance.StressMongo.Caf.Messages;
using Rebus.Tests.Performance.StressMongo.Crm.Messages;

namespace Rebus.Tests.Performance.StressMongo.Caf
{
    class CheckCreditSaga
        : Saga<CheckCreditSagaData>,
          IAmInitiatedBy<CustomerCreated>,
          IHandleMessages<SimulatedCreditCheckComplete>
    {
        readonly IBus bus;
        readonly IFlowLog flowLog;
        readonly Random random = new Random();

        public CheckCreditSaga(IBus bus, IFlowLog flowLog)
        {
            this.bus = bus;
            this.flowLog = flowLog;
        }

        public override void ConfigureHowToFindSaga()
        {
            Incoming<CustomerCreated>(m => m.CustomerId).CorrelatesWith(d => d.CustomerInfo.CustomerId);
            Incoming<SimulatedCreditCheckComplete>(m => m.CustomerId).CorrelatesWith(d => d.CustomerInfo.CustomerId);
        }

        public void Handle(CustomerCreated message)
        {
            // we're idempotent!
            if (Data.CustomerInfo != null) return;

            var customerId = message.CustomerId;

            flowLog.LogFlow(customerId, "Commencing credit check of {0}", message.Name);

            Data.CustomerInfo = new CustomerInfo
                                    {
                                        CustomerId = customerId,
                                        CustomerName = message.Name,
                                    };

            bus.Defer(random.Next(5).Seconds() + 5.Seconds(),
                      new SimulatedCreditCheckComplete {CustomerId = customerId});
        }

        public void Handle(SimulatedCreditCheckComplete message)
        {
            flowLog.LogFlow(message.CustomerId, "Credit check completed!");

            bus.Publish(new CustomerCreditCheckComplete{CustomerId = message.CustomerId});

            MarkAsComplete();
        }
    }
}