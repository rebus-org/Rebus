using Raven.Client.Indexes;
using System.Linq;

namespace Rebus.RavenDb.Timouts
{
    /// <summary>
    /// RavenDb Index to query due timouts
    /// </summary>
    public class TimeoutIndex : AbstractIndexCreationTask<Timeout>
    {
        public TimeoutIndex()
        {
            Map = timeouts => from timeout in timeouts
                              select new
                              {
                                  Id = timeout.Id,
                                  DueTimeUtc = timeout.DueTimeUtc
                              };
        }
    }
}