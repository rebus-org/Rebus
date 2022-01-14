using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus.Sagas;

namespace Rebus.Auditing.Sagas;

/// <summary>
/// Saga snapshot storage that archives a snapshot of the given saga data
/// </summary>
public interface ISagaSnapshotStorage
{
    /// <summary>
    /// Archives the given saga data under its current ID and revision
    /// </summary>
    Task Save(ISagaData sagaData, Dictionary<string, string> sagaAuditMetadata);
}