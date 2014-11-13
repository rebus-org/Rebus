using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rebus.IdempotentSagas
{
    /// <summary>
    /// A message processed by an idempotent saga.
    /// </summary>
    public class ProcessedMessage
    {
        /// <summary>
        /// A transport message sent during the handling of the processed message.
        /// </summary>
        public class Consecuence
        {
            /// <summary>
            /// The destinations of the message.
            /// </summary>
            public IEnumerable<string> Destinations { get; set; }
            /// <summary>
            /// The headers of the message.
            /// </summary>
            public IDictionary<string, object> Headers { get; set; }
            /// <summary>
            /// The logical messages inside the transport message.
            /// </summary>
            public IDictionary<string, string> Messages { get; set; }
        }

        public ProcessedMessage()
        {
            Consecuences = new List<Consecuence>();
        }

        /// <summary>
        /// The id of the processed message.
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// The message.
        /// </summary>
        public object Message { get; set; }
        /// <summary>
        /// The messages sent during the processed message handling.
        /// </summary>
        public IList<Consecuence> Consecuences { get; set; }
    }
}
