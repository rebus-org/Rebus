using System.Collections.Generic;
using Rebus.Auditing.Sagas;
using Rebus.Sagas;

namespace Rebus.Tests.Contracts.Sagas;

public interface ISagaSnapshotStorageFactory
{
    ISagaSnapshotStorage Create();

    IEnumerable<SagaDataSnapshot> GetAllSnapshots();
}

public class SagaDataSnapshot
{
    public ISagaData SagaData { get; set; }
    public Dictionary<string,string> Metadata { get; set; }
}