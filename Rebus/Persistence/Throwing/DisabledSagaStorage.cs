using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus.Sagas;

namespace Rebus.Persistence.Throwing;

class DisabledSagaStorage : ISagaStorage
{
    public Task<ISagaData> Find(Type sagaDataType, string propertyName, object propertyValue) => throw GetException();

    public Task Insert(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties) => throw GetException();

    public Task Update(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties) => throw GetException();

    public Task Delete(ISagaData sagaData) => throw GetException();

    static InvalidOperationException GetException() => new(@"A saga storage has not been configured. Please configure a saga storage with the .Sagas(...) configurer, e.g. like so:

Configure.With(..)
    .(...)
    .Sagas(s => s.StoreInMemory())
    .(...)

in order to save sagas in memory, or something like 

Configure.With(..)
    .(...)
    .Sagas(s => s.StoreSqlServer(...))
    .(...)

if you have imported the Rebus.SqlServer package and want to store sagas in SQL Server.
");
}