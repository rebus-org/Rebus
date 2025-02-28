using System;

namespace Rebus.Sagas.Ids;

/// <summary>
/// Not a fan of factory factories, but somehow it has become relevant here 😅
/// </summary>
static class DefaultSagaIdFactoryFactory
{
    /// <summary>
    /// Gets the default <see cref="ISagaDataIdFactory"/> implementation, which is based on .NET 9's ability to generate v7 Guids when running on .NET 9
    /// and later, and based on <see cref="SqlServerSagaIdFactory"/> on versions prior to 9.
    /// </summary>
    public static ISagaDataIdFactory GetDefault()
    {
#if HASGUIDV7
        return new Version7SagaIdFactory();
#else
        return new SqlServerSagaIdFactory();
#endif
    }

#if HASGUIDV7
    class Version7SagaIdFactory : ISagaDataIdFactory
    {
        public Guid NewId() => Guid.CreateVersion7();
    }
#else
    class Version7SagaIdFactory : ISagaDataIdFactory
    {
        public Guid NewId() => throw new NotSupportedException("Sorry, but this saga ID factory can only be used in .NET 9 and later");
    }
#endif
}