using System.Linq;
using Raven.Client.Indexes;

namespace Rebus.RavenDb.Timouts
{
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