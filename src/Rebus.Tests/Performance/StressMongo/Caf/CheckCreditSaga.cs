using System;
using Rebus.Tests.Performance.StressMongo.Crm;

namespace Rebus.Tests.Performance.StressMongo.Caf
{
    internal class CheckCreditSaga
        : Saga<CheckCreditSagaData>,
          IAmInitiatedBy<CustomerCreated>,
          IHandleMessages<CreditCheckComplete>
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
            Incoming<CreditCheckComplete>(m => m.CustomerId).CorrelatesWith(d => d.CustomerInfo.CustomerId);
        }

        public void Handle(CustomerCreated message)
        {
            // if saga is new, store the ID and pretend to do some work
            if (Data.CustomerInfo == null)
            {
                var customerId = message.CustomerId;

                flowLog.Log(customerId, "Commencing credit check of {0}", message.Name);

                Data.CustomerInfo = new CustomerInfo
                                        {
                                            CustomerId = customerId,
                                            CustomerName = message.Name,
                                        };

                bus.Defer(random.Next(5).Seconds() + 5.Seconds(),
                          new CreditCheckComplete {CustomerId = customerId});
            }
        }

        public void Handle(CreditCheckComplete message)
        {
            flowLog.Log(message.CustomerId, "Credit check completed!");

            MarkAsComplete();
        }
    }

    class CreditCheckComplete
    {
        public Guid CustomerId { get; set; }
    }
}