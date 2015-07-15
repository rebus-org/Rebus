using System;

namespace Rebus.Sagas.Idempotent
{
    /// <summary>
    /// Convenient standard implementation of <see cref="IIdempotentSagaData"/>
    /// </summary>
    public abstract class IdempotentSagaData : IIdempotentSagaData
    {
        public Guid Id { get; set; }
        public int Revision { get; set; }
        public IdempotencyData IdempotencyData { get; set; }
    }
}