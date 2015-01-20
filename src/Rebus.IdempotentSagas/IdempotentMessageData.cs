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
    public class IdempotentMessageData
    {
        #region SideEffects inner class
        /// <summary>
        /// A transport message sent during the handling of the processed message.
        /// </summary>
        public class SideEffect
        {
            /// <summary>
            /// The destinations of the message.
            /// </summary>
            public IEnumerable<string> Destinations { get; private set; }
            /// <summary>
            /// The headers of the message.
            /// </summary>
            public IDictionary<string, object> Headers { get; private set; }
            /// <summary>
            /// The logical messages inside the transport message.
            /// </summary>
            public IDictionary<string, string> Messages { get; private set; }

            public SideEffect()
            {
            }

            public SideEffect(IEnumerable<string> destinations, IDictionary<string, object> headers, IDictionary<string, string> messages)
            {
                Destinations = destinations;
                Headers = headers;
                Messages = messages;
            }
        }
        #endregion

        /// <summary>
        /// The id of the processed message.
        /// </summary>
        public string Id { get; private set; }
        /// <summary>
        /// The message.
        /// </summary>
        public object Message { get; private set; }
        /// <summary>
        /// The messages sent during the processed message handling.
        /// </summary>
        public IList<SideEffect> SideEffects { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="IdempotentMessageData"/> class.
        /// </summary>
        public IdempotentMessageData()
        {
            SideEffects = new List<SideEffect>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IdempotentMessageData"/> class.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="message">The message.</param>
        public IdempotentMessageData(string id, object message)
            : base()
        {
            Id = id;
            Message = message;
        }
    }
}
