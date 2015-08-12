using System;

namespace Rebus.Tests.Performance.StressMongo.Crm.Messages
{
    public class CustomerCreated
    {
        public Guid CustomerId { get; set; }
        public string Name { get; set; }
    }
}