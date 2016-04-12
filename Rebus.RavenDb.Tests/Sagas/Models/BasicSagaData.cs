using System;
using Rebus.Sagas;

namespace Rebus.RavenDb.Tests.Sagas.Models
{
    public class BasicSagaData : ISagaData
    {
        public Guid Id { get; set; }
        public int Revision { get; set; }

        public string StringField { get; set; }

        public int IntegerField { get; set; }
    }
}