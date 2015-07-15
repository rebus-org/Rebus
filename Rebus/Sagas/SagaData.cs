using System;

namespace Rebus.Sagas
{
    /// <summary>
    /// Convenient implementation of <see cref="ISagaData"/>
    /// </summary>
    public abstract class SagaData : ISagaData
    {
        public Guid Id { get; set; }
        public int Revision { get; set; }
    }
}