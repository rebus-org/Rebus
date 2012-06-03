using System;

namespace Rebus.Tests.Performance.StressMongo.Caf
{
    class CheckCreditSagaData : ISagaData
    {
        public Guid Id { get; set; }
        public int Revision { get; set; }

        public CustomerInfo CustomerInfo { get; set; }
    }

    class CustomerInfo
    {
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; }
    }
}