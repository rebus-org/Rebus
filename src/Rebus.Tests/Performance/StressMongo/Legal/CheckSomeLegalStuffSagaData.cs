using System;

namespace Rebus.Tests.Performance.StressMongo.Legal
{
    public class CheckSomeLegalStuffSagaData : ISagaData
    {
        public Guid Id { get; set; }
        public int Revision { get; set; }

        public Guid CustomerId { get; set; }
    }
}