using System;
using System.Collections.Generic;

namespace Rebus.RavenDb.Timouts
{
    /// <summary>
    /// RavenDb document to contain Timout information
    /// </summary>
    public class Timeout
    {
        public Timeout(Dictionary<string, string> headers, byte[] body, DateTime dueTimeUtc)
        {
            Headers = headers;
            Body = body;
            DueTimeUtc = dueTimeUtc;
            OriginalDueTimeUtc = dueTimeUtc;
        }

        public string Id { get; protected set; }
        public Dictionary<string, string> Headers { get; protected set; }
        public byte[] Body { get; protected set; }
        public DateTime DueTimeUtc { get; protected set; }
        public DateTime OriginalDueTimeUtc { get; protected set; }
    }
}