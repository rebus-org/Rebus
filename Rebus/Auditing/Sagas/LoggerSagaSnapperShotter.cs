using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Rebus.Logging;
using Rebus.Sagas;
#pragma warning disable 1998

namespace Rebus.Auditing.Sagas;

class LoggerSagaSnapperShotter : ISagaSnapshotStorage
{
    readonly ILog _log;

    public LoggerSagaSnapperShotter(IRebusLoggerFactory rebusLoggerFactory)
    {
        _log = rebusLoggerFactory.GetLogger<LoggerSagaSnapperShotter>();
    }

    public async Task Save(ISagaData sagaData, Dictionary<string, string> sagaAuditMetadata)
    {
        var logData = new
        {
            Data = sagaData,
            Metadata = sagaAuditMetadata
        };
            
        var jsonText = JsonConvert.SerializeObject(logData, Formatting.None);

        _log.Info("{jsonText}", jsonText);
    }
}