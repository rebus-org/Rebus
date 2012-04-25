using System;

namespace Rebus.Tests.Performance.StressMongo.Crm
{
    class CustomerCreated
    {
        public Guid CustomerId { get; set; }
        public string Name { get; set; }
    }
}