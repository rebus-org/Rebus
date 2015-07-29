using System;
using Rebus.Tests.Performance.StressMongo.Caf;
using Rebus.Tests.Performance.StressMongo.Crm.Messages;
using Rebus.Tests.Performance.StressMongo.Legal;

namespace Rebus.Tests.Performance.StressMongo.Dcc
{
    public class MaintainCustomerInformationSaga
        : Saga<CustomerInformationSagaData>,
          IAmInitiatedBy<CustomerCreated>,
          IHandleMessages<CustomerCreditCheckComplete>,
          IHandleMessages<CustomerLegallyApproved>
    {
        readonly IFlowLog flowLog;

        public MaintainCustomerInformationSaga(IFlowLog flowLog)
        {
            this.flowLog = flowLog;
        }

        public override void ConfigureHowToFindSaga()
        {
            Incoming<CustomerCreated>(m => m.CustomerId).CorrelatesWith(d => d.Customer.CustomerId);
            Incoming<CustomerCreditCheckComplete>(m => m.CustomerId).CorrelatesWith(d => d.Customer.CustomerId);
            Incoming<CustomerLegallyApproved>(m => m.CustomerId).CorrelatesWith(d => d.Customer.CustomerId);
        }

        public void Handle(CustomerCreated message)
        {
            if (Data.Customer != null) return;

            flowLog.LogFlow(message.CustomerId, "Created customer saga for {0}", message.Name);

            Data.Customer = new CustomerInformation
                                {
                                    CustomerId = message.CustomerId,
                                    Name = message.Name,
                                };
        }

        public void Handle(CustomerCreditCheckComplete message)
        {
            flowLog.LogFlow(message.CustomerId, "Credit check for {0} complete", Data.Customer.Name);

            Data.CreditStatus.Complete = true;
        }

        public void Handle(CustomerLegallyApproved message)
        {
            flowLog.LogFlow(message.CustomerId, "Legal check for {0} complete", Data.Customer.Name);

            Data.LegalStatus.Complete = true;
        }
    }

    public class CustomerInformationSagaData : ISagaData
    {
        public CustomerInformationSagaData()
        {
            LegalStatus = new LegalStatus();
            CreditStatus = new CreditStatus();
        }

        public Guid Id { get; set; }
        public int Revision { get; set; }
        
        public CustomerInformation Customer { get; set; }
        public CreditStatus CreditStatus { get; set; }
        public LegalStatus LegalStatus { get; set; }
    }

    public class CustomerInformation
    {
        public Guid CustomerId { get; set; }
        public string Name { get; set; }
    }

    public class CreditStatus
    {
        public bool Complete { get; set; }
    }

    public class LegalStatus
    {
        public bool Complete { get; set; }
    }
}